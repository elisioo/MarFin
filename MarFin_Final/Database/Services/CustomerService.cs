using MarFin_Final.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;

namespace MarFin_Final.Data
{
    public class CustomerService
    {
        // Pagination model
        public class PaginatedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
            public bool HasPrevious => PageNumber > 1;
            public bool HasNext => PageNumber < TotalPages;
        }

        // GET PAGINATED CUSTOMERS
        public PaginatedResult<Customer> GetCustomersPaginated(
            int pageNumber = 1,
            int pageSize = 20,
            string searchTerm = "",
            string statusFilter = "",
            bool isArchived = false)
        {
            var result = new PaginatedResult<Customer>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Build WHERE clause
                    string whereClause = "WHERE c.is_archived = @IsArchived AND c.is_active = 1";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        whereClause += @" AND (c.first_name LIKE @SearchTerm 
                                            OR c.last_name LIKE @SearchTerm 
                                            OR c.email LIKE @SearchTerm 
                                            OR c.company_name LIKE @SearchTerm
                                            OR c.phone LIKE @SearchTerm)";
                    }

                    if (!string.IsNullOrWhiteSpace(statusFilter))
                    {
                        whereClause += " AND c.customer_status = @StatusFilter";
                    }

                    // Get total count
                    string countQuery = $@"SELECT COUNT(*) 
                                          FROM tbl_Customers c 
                                          {whereClause}";

                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        countCmd.Parameters.AddWithValue("@IsArchived", isArchived ? 1 : 0);

                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            countCmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                        }

                        if (!string.IsNullOrWhiteSpace(statusFilter))
                        {
                            countCmd.Parameters.AddWithValue("@StatusFilter", statusFilter);
                        }

                        result.TotalCount = (int)countCmd.ExecuteScalar();
                    }

                    // Get paginated data
                    string dataQuery = $@"
                        SELECT c.customer_id, c.segment_id, c.created_by, c.modified_by,
                               c.first_name, c.last_name, c.email, c.phone, c.company_name,
                               c.address, c.city, c.state_province, c.postal_code, c.country,
                               c.customer_status, c.total_revenue, c.source, c.notes,
                               c.is_active, c.is_archived, c.archived_date, c.archived_by,
                               c.created_date, c.modified_date,
                               s.segment_name
                        FROM tbl_Customers c
                        LEFT JOIN tbl_Customer_Segments s ON c.segment_id = s.segment_id
                        {whereClause}
                        ORDER BY c.company_name, c.last_name, c.first_name
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand dataCmd = new SqlCommand(dataQuery, conn))
                    {
                        dataCmd.Parameters.AddWithValue("@IsArchived", isArchived ? 1 : 0);
                        dataCmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            dataCmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                        }

                        if (!string.IsNullOrWhiteSpace(statusFilter))
                        {
                            dataCmd.Parameters.AddWithValue("@StatusFilter", statusFilter);
                        }

                        using (SqlDataReader reader = dataCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Items.Add(MapCustomerFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting paginated customers: " + ex.Message);
            }

            return result;
        }

        // CREATE - Add new customer
        public bool AddCustomer(Customer customer)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO tbl_Customers 
                                   (segment_id, created_by, first_name, last_name, email, phone, 
                                    company_name, address, city, state_province, postal_code, country, 
                                    customer_status, total_revenue, source, notes, is_active, 
                                    created_date, modified_date) 
                                   VALUES 
                                   (@SegmentId, @CreatedBy, @FirstName, @LastName, @Email, @Phone, 
                                    @CompanyName, @Address, @City, @StateProvince, @PostalCode, @Country, 
                                    @CustomerStatus, @TotalRevenue, @Source, @Notes, @IsActive, 
                                    @CreatedDate, @ModifiedDate)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SegmentId", customer.SegmentId);
                        cmd.Parameters.AddWithValue("@CreatedBy", customer.CreatedBy);
                        cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                        cmd.Parameters.AddWithValue("@Email", customer.Email);
                        cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? "");
                        cmd.Parameters.AddWithValue("@CompanyName", customer.CompanyName ?? "");
                        cmd.Parameters.AddWithValue("@Address", customer.Address ?? "");
                        cmd.Parameters.AddWithValue("@City", customer.City ?? "");
                        cmd.Parameters.AddWithValue("@StateProvince", customer.StateProvince ?? "");
                        cmd.Parameters.AddWithValue("@PostalCode", customer.PostalCode ?? "");
                        cmd.Parameters.AddWithValue("@Country", customer.Country);
                        cmd.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                        cmd.Parameters.AddWithValue("@TotalRevenue", customer.TotalRevenue);
                        cmd.Parameters.AddWithValue("@Source", customer.Source ?? "");
                        cmd.Parameters.AddWithValue("@Notes", customer.Notes ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", customer.IsActive);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding customer: " + ex.Message);
                return false;
            }
        }

        // Search customers for autocomplete (limit results)
        public async Task<List<Customer>> SearchCustomersAsync(string searchTerm, int maxResults = 50)
        {
            var customers = new List<Customer>();
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string query = @"
                SELECT TOP (@MaxResults)
                    customer_id, first_name, last_name, company_name, email
                FROM tbl_Customers
                WHERE is_active = 1 AND is_archived = 0
                  AND (
                        first_name LIKE '%' + @Search + '%'
                     OR last_name LIKE '%' + @Search + '%'
                     OR company_name LIKE '%' + @Search + '%'
                     OR email LIKE '%' + @Search + '%'
                     OR phone LIKE '%' + @Search + '%'
                  )
                ORDER BY 
                    CASE 
                        WHEN company_name LIKE @Search + '%' THEN 0
                        WHEN company_name LIKE '%' + @Search + '%' THEN 1
                        ELSE 2 
                    END,
                    company_name, last_name, first_name";

                    using var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Search", searchTerm ?? "");
                    cmd.Parameters.AddWithValue("@MaxResults", maxResults);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        customers.Add(new Customer
                        {
                            CustomerId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            CompanyName = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Email = reader.GetString(4)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Search error: " + ex.Message);
            }
            return customers;
        }

        // READ - Get all active customers (kept for backward compatibility, but consider using paginated version)
        public List<Customer> GetAllCustomers()
        {
            List<Customer> customers = new List<Customer>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.customer_id, c.segment_id, c.created_by, c.modified_by,
                                    c.first_name, c.last_name, c.email, c.phone, c.company_name,
                                    c.address, c.city, c.state_province, c.postal_code, c.country,
                                    c.customer_status, c.total_revenue, c.source, c.notes,
                                    c.is_active, c.is_archived, c.archived_date, c.archived_by,
                                    c.created_date, c.modified_date,
                                    s.segment_name
                                   FROM tbl_Customers c
                                   LEFT JOIN tbl_Customer_Segments s ON c.segment_id = s.segment_id
                                   WHERE c.is_archived = 0 AND c.is_active = 1
                                   ORDER BY c.company_name, c.last_name, c.first_name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                customers.Add(MapCustomerFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting customers: " + ex.Message);
            }

            return customers;
        }

        // READ - Get customer by ID
        public Customer GetCustomerById(int customerId)
        {
            Customer customer = null;

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.customer_id, c.segment_id, c.created_by, c.modified_by,
                                    c.first_name, c.last_name, c.email, c.phone, c.company_name,
                                    c.address, c.city, c.state_province, c.postal_code, c.country,
                                    c.customer_status, c.total_revenue, c.source, c.notes,
                                    c.is_active, c.is_archived, c.archived_date, c.archived_by,
                                    c.created_date, c.modified_date,
                                    s.segment_name
                                   FROM tbl_Customers c
                                   LEFT JOIN tbl_Customer_Segments s ON c.segment_id = s.segment_id
                                   WHERE c.customer_id = @CustomerId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customer = MapCustomerFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting customer: " + ex.Message);
            }

            return customer;
        }

        // UPDATE - Update existing customer
        public bool UpdateCustomer(Customer customer)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Customers 
                                   SET segment_id = @SegmentId,
                                       modified_by = @ModifiedBy,
                                       first_name = @FirstName,
                                       last_name = @LastName,
                                       email = @Email,
                                       phone = @Phone,
                                       company_name = @CompanyName,
                                       address = @Address,
                                       city = @City,
                                       state_province = @StateProvince,
                                       postal_code = @PostalCode,
                                       country = @Country,
                                       customer_status = @CustomerStatus,
                                       source = @Source,
                                       notes = @Notes,
                                       is_active = @IsActive,
                                       modified_date = @ModifiedDate
                                   WHERE customer_id = @CustomerId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                        cmd.Parameters.AddWithValue("@SegmentId", customer.SegmentId);
                        cmd.Parameters.AddWithValue("@ModifiedBy", customer.ModifiedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                        cmd.Parameters.AddWithValue("@Email", customer.Email);
                        cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? "");
                        cmd.Parameters.AddWithValue("@CompanyName", customer.CompanyName ?? "");
                        cmd.Parameters.AddWithValue("@Address", customer.Address ?? "");
                        cmd.Parameters.AddWithValue("@City", customer.City ?? "");
                        cmd.Parameters.AddWithValue("@StateProvince", customer.StateProvince ?? "");
                        cmd.Parameters.AddWithValue("@PostalCode", customer.PostalCode ?? "");
                        cmd.Parameters.AddWithValue("@Country", customer.Country);
                        cmd.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                        cmd.Parameters.AddWithValue("@Source", customer.Source ?? "");
                        cmd.Parameters.AddWithValue("@Notes", customer.Notes ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", customer.IsActive);
                        cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating customer: " + ex.Message);
                return false;
            }
        }

        // ARCHIVE - Archive customer
        public bool ArchiveCustomer(int customerId, int archivedBy)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Customers 
                                   SET is_archived = 1, 
                                       archived_date = @ArchivedDate,
                                       archived_by = @ArchivedBy
                                   WHERE customer_id = @CustomerId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        cmd.Parameters.AddWithValue("@ArchivedDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ArchivedBy", archivedBy);

                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error archiving customer: " + ex.Message);
                return false;
            }
        }

        // RESTORE - Restore archived customer
        public bool RestoreCustomer(int customerId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Customers 
                                   SET is_archived = 0, 
                                       archived_date = NULL,
                                       archived_by = NULL
                                   WHERE customer_id = @CustomerId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error restoring customer: " + ex.Message);
                return false;
            }
        }

        // GET ARCHIVED - Get all archived customers (consider pagination for large datasets)
        public List<Customer> GetArchivedCustomers()
        {
            List<Customer> customers = new List<Customer>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT c.customer_id, c.segment_id, c.created_by, c.modified_by,
                                    c.first_name, c.last_name, c.email, c.phone, c.company_name,
                                    c.address, c.city, c.state_province, c.postal_code, c.country,
                                    c.customer_status, c.total_revenue, c.source, c.notes,
                                    c.is_active, c.is_archived, c.archived_date, c.archived_by,
                                    c.created_date, c.modified_date,
                                    s.segment_name
                                   FROM tbl_Customers c
                                   LEFT JOIN tbl_Customer_Segments s ON c.segment_id = s.segment_id
                                   WHERE c.is_archived = 1
                                   ORDER BY c.archived_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                customers.Add(MapCustomerFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting archived customers: " + ex.Message);
            }

            return customers;
        }

        // DELETE - Permanently delete customer (use with caution)
        public bool DeleteCustomer(int customerId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM tbl_Customers WHERE customer_id = @CustomerId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting customer: " + ex.Message);
                return false;
            }
        }

        // Get customer count
        public int GetCustomerCount()
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM tbl_Customers WHERE is_archived = 0 AND is_active = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting customer count: " + ex.Message);
                return 0;
            }
        }

        // Helper method to map data reader to Customer object
        private Customer MapCustomerFromReader(SqlDataReader reader)
        {
            return new Customer
            {
                CustomerId = Convert.ToInt32(reader["customer_id"]),
                SegmentId = Convert.ToInt32(reader["segment_id"]),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                ModifiedBy = reader["modified_by"] != DBNull.Value ? Convert.ToInt32(reader["modified_by"]) : null,
                FirstName = reader["first_name"]?.ToString() ?? "",
                LastName = reader["last_name"]?.ToString() ?? "",
                Email = reader["email"]?.ToString() ?? "",
                Phone = reader["phone"]?.ToString() ?? "",
                CompanyName = reader["company_name"]?.ToString() ?? "",
                Address = reader["address"]?.ToString() ?? "",
                City = reader["city"]?.ToString() ?? "",
                StateProvince = reader["state_province"]?.ToString() ?? "",
                PostalCode = reader["postal_code"]?.ToString() ?? "",
                Country = reader["country"]?.ToString() ?? "Philippines",
                CustomerStatus = reader["customer_status"]?.ToString() ?? "Lead",
                TotalRevenue = reader["total_revenue"] != DBNull.Value ? Convert.ToDecimal(reader["total_revenue"]) : 0m,
                Source = reader["source"]?.ToString() ?? "",
                Notes = reader["notes"]?.ToString() ?? "",
                IsActive = Convert.ToBoolean(reader["is_active"]),
                IsArchived = Convert.ToBoolean(reader["is_archived"]),
                ArchivedDate = reader["archived_date"] != DBNull.Value ? Convert.ToDateTime(reader["archived_date"]) : null,
                ArchivedBy = reader["archived_by"] != DBNull.Value ? Convert.ToInt32(reader["archived_by"]) : null,
                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                ModifiedDate = Convert.ToDateTime(reader["modified_date"]),
                CustomerSegment = reader["segment_name"]?.ToString() ?? ""
            };
        }
    }
}