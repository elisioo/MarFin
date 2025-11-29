using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using MarFin_Final.Models;

namespace MarFin_Final.Database.Services
{
    public class RemoteDatabaseService
    {
        private readonly string _connectionString;

        public RemoteDatabaseService()
        {
            _connectionString = "Server=db33549.public.databaseasp.net;" +
                              "Database=db33549;" +
                              "User Id=db33549;" +
                              "Password=marfindbit13;" +
                              "Encrypt=True;" +
                              "TrustServerCertificate=True;" +
                              "Connection Timeout=30;" +
                              "MultipleActiveResultSets=True;";
        }

        // CRITICAL: Comprehensive diagnostic method
        public async Task<SyncDiagnostic> RunComprehensiveDiagnosticAsync()
        {
            var diagnostic = new SyncDiagnostic();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    diagnostic.AddStep("Testing connection...");
                    await connection.OpenAsync();
                    diagnostic.AddStep("✓ Connection successful");

                    // Check if tables exist
                    diagnostic.AddStep("Checking if tbl_Customers exists...");
                    var tableQuery = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                                      WHERE TABLE_NAME = 'tbl_Customers'";
                    using (SqlCommand cmd = new SqlCommand(tableQuery, connection))
                    {
                        var tableExists = (int)await cmd.ExecuteScalarAsync() > 0;
                        diagnostic.AddStep(tableExists ? "✓ tbl_Customers exists" : "✗ tbl_Customers does NOT exist");
                        diagnostic.TableExists = tableExists;
                    }

                    if (diagnostic.TableExists)
                    {
                        // Check table structure
                        diagnostic.AddStep("Checking table structure...");
                        var structureQuery = @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                                              FROM INFORMATION_SCHEMA.COLUMNS 
                                              WHERE TABLE_NAME = 'tbl_Customers'
                                              ORDER BY ORDINAL_POSITION";
                        using (SqlCommand cmd = new SqlCommand(structureQuery, connection))
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var columns = new List<string>();
                            while (await reader.ReadAsync())
                            {
                                columns.Add($"{reader["COLUMN_NAME"]} ({reader["DATA_TYPE"]}, Nullable: {reader["IS_NULLABLE"]})");
                            }
                            diagnostic.AddStep($"✓ Table has {columns.Count} columns:");
                            foreach (var col in columns)
                            {
                                diagnostic.AddStep($"  - {col}");
                            }
                        }

                        // Check current record count
                        diagnostic.AddStep("Checking current record count...");
                        var countQuery = "SELECT COUNT(*) FROM tbl_Customers";
                        using (SqlCommand cmd = new SqlCommand(countQuery, connection))
                        {
                            var count = (int)await cmd.ExecuteScalarAsync();
                            diagnostic.AddStep($"✓ Current records in remote: {count}");
                            diagnostic.RemoteRecordCount = count;
                        }

                        // Test INSERT permission
                        diagnostic.AddStep("Testing INSERT permission with sample data...");
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                var testQuery = @"INSERT INTO tbl_Customers 
                                    (segment_id, created_by, first_name, last_name, email, phone, 
                                     company_name, address, city, state_province, postal_code, country, 
                                     customer_status, total_revenue, source, notes, is_active, 
                                     is_archived, created_date, modified_date) 
                                    VALUES 
                                    (1, 1, 'TEST', 'USER', 'test@test.com', '0000000000',
                                     'Test Company', 'Test Address', 'Test City', 'Test State', '0000', 'Philippines',
                                     'Lead', 0, 'Test', 'Diagnostic test', 1, 0, GETDATE(), GETDATE())";

                                using (SqlCommand cmd = new SqlCommand(testQuery, connection, transaction))
                                {
                                    await cmd.ExecuteNonQueryAsync();
                                    diagnostic.AddStep("✓ INSERT permission verified (test rolled back)");
                                    diagnostic.CanInsert = true;
                                }

                                transaction.Rollback(); // Always rollback test
                            }
                            catch (SqlException ex)
                            {
                                transaction.Rollback();
                                diagnostic.AddStep($"✗ INSERT failed: {ex.Message}");
                                diagnostic.AddStep($"  Error Number: {ex.Number}");
                                diagnostic.CanInsert = false;

                                // Specific error handling
                                if (ex.Number == 547) // Foreign key constraint
                                {
                                    diagnostic.AddStep("  Issue: Foreign key constraint violation");
                                    diagnostic.AddStep("  Solution: Ensure segment_id=1 exists in tbl_Customer_Segments");
                                }
                                else if (ex.Number == 2627 || ex.Number == 2601) // Unique constraint
                                {
                                    diagnostic.AddStep("  Issue: Duplicate key violation");
                                }
                                else if (ex.Number == 229) // Permission denied
                                {
                                    diagnostic.AddStep("  Issue: No INSERT permission on table");
                                }
                            }
                        }
                    }
                    else
                    {
                        diagnostic.AddStep("✗ Cannot proceed - table does not exist");
                    }
                }

                diagnostic.Success = diagnostic.TableExists && diagnostic.CanInsert;
            }
            catch (SqlException sqlEx)
            {
                diagnostic.AddStep($"✗ SQL Error: {sqlEx.Message}");
                diagnostic.AddStep($"  Error Number: {sqlEx.Number}");
                diagnostic.Success = false;
            }
            catch (Exception ex)
            {
                diagnostic.AddStep($"✗ Error: {ex.Message}");
                diagnostic.Success = false;
            }

            return diagnostic;
        }

        // IMPROVED: Sync customers with detailed logging
        public async Task<int> SyncCustomersToRemoteAsync(List<Customer> customers)
        {
            int syncedCount = 0;

            if (customers == null || customers.Count == 0)
            {
                Console.WriteLine("⚠ RemoteDatabaseService: No customers provided to sync");
                return 0;
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"SYNC STARTING: {customers.Count} customers to process");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("✓ Connection opened successfully");

                    // First, ensure at least one segment exists
                    await EnsureSegmentExistsAsync(connection);

                    foreach (var customer in customers)
                    {
                        try
                        {
                            // Check if customer exists by email
                            string checkQuery = "SELECT customer_id FROM tbl_Customers WHERE email = @email";
                            int? existingCustomerId = null;

                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@email", customer.Email ?? "");
                                var result = await checkCmd.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    existingCustomerId = Convert.ToInt32(result);
                                }
                            }

                            string query;
                            SqlCommand command;

                            if (existingCustomerId.HasValue)
                            {
                                // UPDATE existing customer
                                Console.WriteLine($"  Updating: {customer.FirstName} {customer.LastName} (ID: {existingCustomerId.Value})");
                                query = @"UPDATE tbl_Customers SET 
                                            segment_id = @segment_id,
                                            first_name = @first_name,
                                            last_name = @last_name,
                                            phone = @phone,
                                            company_name = @company_name,
                                            address = @address,
                                            city = @city,
                                            state_province = @state_province,
                                            postal_code = @postal_code,
                                            country = @country,
                                            customer_status = @customer_status,
                                            total_revenue = @total_revenue,
                                            source = @source,
                                            notes = @notes,
                                            is_active = @is_active,
                                            modified_date = GETDATE()
                                        WHERE customer_id = @customer_id";

                                command = new SqlCommand(query, connection);
                                command.Parameters.AddWithValue("@customer_id", existingCustomerId.Value);
                            }
                            else
                            {
                                // INSERT new customer
                                Console.WriteLine($"  Inserting: {customer.FirstName} {customer.LastName} ({customer.Email})");
                                query = @"INSERT INTO tbl_Customers (
                                            segment_id, created_by, first_name, last_name, email, 
                                            phone, company_name, address, city, state_province, 
                                            postal_code, country, customer_status, total_revenue, 
                                            source, notes, is_active, is_archived, created_date, modified_date
                                        ) VALUES (
                                            @segment_id, @created_by, @first_name, @last_name, @email,
                                            @phone, @company_name, @address, @city, @state_province,
                                            @postal_code, @country, @customer_status, @total_revenue,
                                            @source, @notes, @is_active, 0, GETDATE(), GETDATE()
                                        )";

                                command = new SqlCommand(query, connection);
                                command.Parameters.AddWithValue("@created_by", customer.CreatedBy);
                            }

                            // Add parameters (common to both INSERT and UPDATE)
                            command.Parameters.AddWithValue("@segment_id", customer.SegmentId);
                            command.Parameters.AddWithValue("@first_name", customer.FirstName ?? "");
                            command.Parameters.AddWithValue("@last_name", customer.LastName ?? "");
                            command.Parameters.AddWithValue("@email", customer.Email ?? "");
                            command.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(customer.Phone) ? DBNull.Value : customer.Phone);
                            command.Parameters.AddWithValue("@company_name", string.IsNullOrWhiteSpace(customer.CompanyName) ? DBNull.Value : customer.CompanyName);
                            command.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(customer.Address) ? DBNull.Value : customer.Address);
                            command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(customer.City) ? DBNull.Value : customer.City);
                            command.Parameters.AddWithValue("@state_province", string.IsNullOrWhiteSpace(customer.StateProvince) ? DBNull.Value : customer.StateProvince);
                            command.Parameters.AddWithValue("@postal_code", string.IsNullOrWhiteSpace(customer.PostalCode) ? DBNull.Value : customer.PostalCode);
                            command.Parameters.AddWithValue("@country", customer.Country ?? "Philippines");
                            command.Parameters.AddWithValue("@customer_status", customer.CustomerStatus ?? "Lead");
                            command.Parameters.AddWithValue("@total_revenue", customer.TotalRevenue);
                            command.Parameters.AddWithValue("@source", string.IsNullOrWhiteSpace(customer.Source) ? DBNull.Value : customer.Source);
                            command.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(customer.Notes) ? DBNull.Value : customer.Notes);
                            command.Parameters.AddWithValue("@is_active", customer.IsActive);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected > 0)
                            {
                                syncedCount++;
                                Console.WriteLine($"    ✓ Success");
                            }
                            else
                            {
                                Console.WriteLine($"    ✗ No rows affected");
                            }
                        }
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine($"    ✗ SQL Error for {customer.Email}:");
                            Console.WriteLine($"      Error {sqlEx.Number}: {sqlEx.Message}");

                            // Provide specific guidance
                            if (sqlEx.Number == 547)
                            {
                                Console.WriteLine($"      Foreign key issue - check segment_id={customer.SegmentId} exists");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Connection Error: {ex.Message}");
                    Console.WriteLine($"  Stack: {ex.StackTrace}");
                }
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"SYNC COMPLETE: {syncedCount} of {customers.Count} customers synced");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            return syncedCount;
        }

        // Helper: Ensure at least one segment exists
        private async Task EnsureSegmentExistsAsync(SqlConnection connection)
        {
            var checkQuery = "SELECT COUNT(*) FROM tbl_Customer_Segments WHERE segment_id = 1";
            using (SqlCommand cmd = new SqlCommand(checkQuery, connection))
            {
                var exists = (int)await cmd.ExecuteScalarAsync() > 0;

                if (!exists)
                {
                    Console.WriteLine("⚠ Default segment missing - creating segment_id=1");
                    var insertQuery = @"INSERT INTO tbl_Customer_Segments 
                        (segment_id, segment_name, description, min_revenue, max_revenue, is_active, created_date)
                        VALUES (1, 'Default', 'Default customer segment', 0, NULL, 1, GETDATE())";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                    {
                        await insertCmd.ExecuteNonQueryAsync();
                        Console.WriteLine("✓ Default segment created");
                    }
                }
            }
        }

        // Original methods preserved...
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return connection.State == ConnectionState.Open;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(bool IsConnected, string Message)> GetConnectionStatusAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return (true, "Connected to remote database");
                }
            }
            catch (SqlException ex)
            {
                return (false, $"Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<SyncResult> SyncAllDataAsync(List<Customer> customers, List<Invoice> invoices)
        {
            var result = new SyncResult();

            try
            {
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine("COMPREHENSIVE SYNC STARTING");
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine($" Data Available:");
                Console.WriteLine($"   - Customers: {customers?.Count ?? 0}");
                Console.WriteLine($"   - Invoices: {invoices?.Count ?? 0}");

                // Sync Customers
                if (customers != null && customers.Count > 0)
                {
                    result.CustomersAttempted = customers.Count;
                    result.CustomersSynced = await SyncCustomersToRemoteAsync(customers);
                }
                else
                {
                    Console.WriteLine("⚠ WARNING: No customers provided to sync!");
                    Console.WriteLine("   → Check if local database has any active, non-archived customers");
                }

                // Sync Invoices (if needed - reuse similar logic)
                if (invoices != null && invoices.Count > 0)
                {
                    result.InvoicesAttempted = invoices.Count;
                    // result.InvoicesSynced = await SyncInvoicesToRemoteAsync(invoices);
                }

                result.TotalSynced = result.CustomersSynced + result.InvoicesSynced;
                result.IsSuccess = result.TotalSynced > 0;

                if (result.TotalSynced > 0)
                {
                    result.Message = $"✓ Sync completed: {result.CustomersSynced} customers and {result.InvoicesSynced} invoices synced successfully";
                }
                else
                {
                    result.Message = "⚠ No records were synced. Run diagnostics to investigate.";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Sync error: {ex.Message}";
                Console.WriteLine($"SYNC ERROR: {ex.Message}");
            }

            return result;
        }

        public async Task<DateTime?> GetLastSyncTimeAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT MAX(modified_date) FROM tbl_Customers";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return (DateTime)result;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<SyncStats> GetSyncStatsAsync()
        {
            var stats = new SyncStats();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM tbl_Customers", connection))
                    {
                        stats.CustomerCount = (int)await command.ExecuteScalarAsync();
                    }
                    using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM tbl_Invoices", connection))
                    {
                        stats.InvoiceCount = (int)await command.ExecuteScalarAsync();
                    }
                }
            }
            catch { }
            return stats;
        }
    }

    // Diagnostic result class
    public class SyncDiagnostic
    {
        public bool Success { get; set; }
        public bool TableExists { get; set; }
        public bool CanInsert { get; set; }
        public int RemoteRecordCount { get; set; }
        public List<string> Steps { get; set; } = new();

        public void AddStep(string step)
        {
            Steps.Add(step);
            Console.WriteLine(step);
        }

        public override string ToString()
        {
            return string.Join("\n", Steps);
        }
    }

    public class SyncStats
    {
        public int CustomerCount { get; set; }
        public int InvoiceCount { get; set; }
        public int TransactionCount { get; set; }
        public DateTime LastSyncTime { get; set; }
    }

    public class SyncResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public int CustomersAttempted { get; set; }
        public int CustomersSynced { get; set; }
        public int InvoicesAttempted { get; set; }
        public int InvoicesSynced { get; set; }
        public int TotalSynced { get; set; }
    }
}