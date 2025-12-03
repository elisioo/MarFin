using MarFin_Final.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarFin_Final.Data
{
    public class InvoiceService
    {
        // CREATE - Add new invoice with line items
        public bool AddInvoice(Invoice invoice)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Always generate a fresh invoice number to avoid duplicates
                            invoice.InvoiceNumber = GenerateInvoiceNumber();

                            // Insert invoice
                            string invoiceQuery = @"INSERT INTO tbl_Invoices 
                                (customer_id, created_by, invoice_number, invoice_date, due_date, 
                                 payment_terms, subtotal, tax_rate, tax_amount, discount_amount, 
                                 total_amount, payment_status, notes, pdf_path, is_archived, 
                                 created_date, modified_date) 
                                OUTPUT INSERTED.invoice_id
                                VALUES 
                                (@CustomerId, @CreatedBy, @InvoiceNumber, @InvoiceDate, @DueDate, 
                                 @PaymentTerms, @Subtotal, @TaxRate, @TaxAmount, @DiscountAmount, 
                                 @TotalAmount, @PaymentStatus, @Notes, @PdfPath, @IsArchived, 
                                 @CreatedDate, @ModifiedDate)";

                            int invoiceId;
                            using (SqlCommand cmd = new SqlCommand(invoiceQuery, conn, transaction))
                            {
                                AddInvoiceParameters(cmd, invoice);
                                invoiceId = (int)cmd.ExecuteScalar();
                            }

                            // Insert line items
                            if (invoice.LineItems != null && invoice.LineItems.Count > 0)
                            {
                                string itemQuery = @"INSERT INTO tbl_Invoice_Items 
                                    (invoice_id, item_order, description, quantity, unit_price, amount)
                                    VALUES (@InvoiceId, @ItemOrder, @Description, @Quantity, @UnitPrice, @Amount)";

                                foreach (var item in invoice.LineItems)
                                {
                                    using (SqlCommand cmd = new SqlCommand(itemQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                                        cmd.Parameters.AddWithValue("@ItemOrder", item.ItemOrder);
                                        cmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                                        cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                                        cmd.Parameters.AddWithValue("@Amount", item.Amount);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding invoice: " + ex.Message);
                return false;
            }
        }

        // READ - Get all active invoices
        public List<Invoice> GetAllInvoices()
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 0
                                ORDER BY i.invoice_date DESC, i.invoice_number DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoices.Add(MapInvoiceFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting invoices: " + ex.Message);
            }

            return invoices;
        }

        // READ - Get invoices by customer ID (for customer invoice history)
        public List<Invoice> GetInvoicesByCustomerId(int customerId)
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 0 AND i.customer_id = @CustomerId
                                ORDER BY i.invoice_date DESC, i.invoice_number DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                invoices.Add(MapInvoiceFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting invoices by customer: " + ex.Message);
            }

            return invoices;
        }

        // READ - Get invoice by ID with line items
        public Invoice GetInvoiceById(int invoiceId)
        {
            Invoice invoice = null;

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Get invoice
                    string invoiceQuery = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.invoice_id = @InvoiceId";

                    using (SqlCommand cmd = new SqlCommand(invoiceQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                invoice = MapInvoiceFromReader(reader);
                            }
                        }
                    }

                    // Get line items
                    if (invoice != null)
                    {
                        string itemsQuery = @"SELECT item_id, invoice_id, item_order, description, 
                                             quantity, unit_price, amount
                                             FROM tbl_Invoice_Items
                                             WHERE invoice_id = @InvoiceId
                                             ORDER BY item_order";

                        using (SqlCommand cmd = new SqlCommand(itemsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    invoice.LineItems.Add(MapInvoiceItemFromReader(reader));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting invoice: " + ex.Message);
            }

            return invoice;
        }

        // UPDATE - Update existing invoice
        public bool UpdateInvoice(Invoice invoice)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Update invoice
                            string invoiceQuery = @"UPDATE tbl_Invoices 
                                SET customer_id = @CustomerId,
                                    invoice_number = @InvoiceNumber,
                                    invoice_date = @InvoiceDate,
                                    due_date = @DueDate,
                                    payment_terms = @PaymentTerms,
                                    subtotal = @Subtotal,
                                    tax_rate = @TaxRate,
                                    tax_amount = @TaxAmount,
                                    discount_amount = @DiscountAmount,
                                    total_amount = @TotalAmount,
                                    payment_status = @PaymentStatus,
                                    notes = @Notes,
                                    pdf_path = @PdfPath,
                                    modified_date = @ModifiedDate
                                WHERE invoice_id = @InvoiceId";

                            using (SqlCommand cmd = new SqlCommand(invoiceQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);
                                AddInvoiceParameters(cmd, invoice);
                                cmd.ExecuteNonQuery();
                            }

                            // Delete existing line items
                            string deleteItemsQuery = "DELETE FROM tbl_Invoice_Items WHERE invoice_id = @InvoiceId";
                            using (SqlCommand cmd = new SqlCommand(deleteItemsQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);
                                cmd.ExecuteNonQuery();
                            }

                            // Insert updated line items
                            if (invoice.LineItems != null && invoice.LineItems.Count > 0)
                            {
                                string itemQuery = @"INSERT INTO tbl_Invoice_Items 
                                    (invoice_id, item_order, description, quantity, unit_price, amount)
                                    VALUES (@InvoiceId, @ItemOrder, @Description, @Quantity, @UnitPrice, @Amount)";

                                foreach (var item in invoice.LineItems)
                                {
                                    using (SqlCommand cmd = new SqlCommand(itemQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);
                                        cmd.Parameters.AddWithValue("@ItemOrder", item.ItemOrder);
                                        cmd.Parameters.AddWithValue("@Description", item.Description ?? "");
                                        cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                                        cmd.Parameters.AddWithValue("@Amount", item.Amount);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating invoice: " + ex.Message);
                return false;
            }
        }

        // ARCHIVE - Archive invoice
        public bool ArchiveInvoice(int invoiceId)
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE tbl_Invoices 
                                   SET is_archived = 1, 
                                       archived_date = @ArchivedDate
                                   WHERE invoice_id = @InvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        cmd.Parameters.AddWithValue("@ArchivedDate", DateTime.Now);
                        int result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error archiving invoice: " + ex.Message);
                return false;
            }
        }

        // GET ARCHIVED - Get archived invoices
        public List<Invoice> GetArchivedInvoices()
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 1
                                ORDER BY i.archived_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoices.Add(MapInvoiceFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting archived invoices: " + ex.Message);
            }

            return invoices;
        }

        // SEARCH - Search invoices
        public List<Invoice> SearchInvoices(string searchTerm)
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 0
                                AND (i.invoice_number LIKE @SearchTerm 
                                     OR c.first_name LIKE @SearchTerm 
                                     OR c.last_name LIKE @SearchTerm 
                                     OR c.company_name LIKE @SearchTerm)
                                ORDER BY i.invoice_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                invoices.Add(MapInvoiceFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error searching invoices: " + ex.Message);
            }

            return invoices;
        }

        // Get invoices by payment status
        public List<Invoice> GetInvoicesByStatus(string status)
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 0 AND i.payment_status = @Status
                                ORDER BY i.invoice_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                invoices.Add(MapInvoiceFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting invoices by status: " + ex.Message);
            }

            return invoices;
        }

        // Get overdue invoices
        public List<Invoice> GetOverdueInvoices()
        {
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT i.invoice_id, i.customer_id, i.created_by, i.invoice_number,
                                    i.invoice_date, i.due_date, i.payment_terms, i.subtotal, i.tax_rate,
                                    i.tax_amount, i.discount_amount, i.total_amount, i.payment_status,
                                    i.notes, i.pdf_path, i.is_archived, i.archived_date,
                                    i.created_date, i.modified_date,
                                    c.first_name + ' ' + c.last_name AS customer_name,
                                    c.email AS customer_email,
                                    c.company_name AS customer_company,
                                    u.first_name + ' ' + u.last_name AS created_by_name
                                FROM tbl_Invoices i
                                INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                                INNER JOIN tbl_Users u ON i.created_by = u.user_id
                                WHERE i.is_archived = 0 
                                AND i.payment_status != 'Paid' 
                                AND i.payment_status != 'Void'
                                AND i.due_date < GETDATE()
                                ORDER BY i.due_date ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoices.Add(MapInvoiceFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting overdue invoices: " + ex.Message);
            }

            return invoices;
        }

        // Get invoice count
        public int GetInvoiceCount()
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM tbl_Invoices WHERE is_archived = 0";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting invoice count: " + ex.Message);
                return 0;
            }
        }

        // Generate next invoice number
        public string GenerateInvoiceNumber()
        {
            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT TOP 1 invoice_number 
                                   FROM tbl_Invoices 
                                   ORDER BY invoice_id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            string lastNumber = result.ToString();
                            // Extract number part (e.g., "INV-2025-006" -> 6)
                            var parts = lastNumber.Split('-');
                            if (parts.Length >= 3 && int.TryParse(parts[2], out int num))
                            {
                                return $"INV-{DateTime.Now.Year}-{(num + 1):D3}";
                            }
                        }
                        return $"INV-{DateTime.Now.Year}-001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating invoice number: " + ex.Message);
                return $"INV-{DateTime.Now.Year}-001";
            }
        }

        // Helper method to add invoice parameters
        private void AddInvoiceParameters(SqlCommand cmd, Invoice invoice)
        {
            cmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId);
            cmd.Parameters.AddWithValue("@CreatedBy", invoice.CreatedBy);
            cmd.Parameters.AddWithValue("@InvoiceNumber", invoice.InvoiceNumber);
            cmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
            cmd.Parameters.AddWithValue("@DueDate", invoice.DueDate);
            cmd.Parameters.AddWithValue("@PaymentTerms", invoice.PaymentTerms ?? "Net 30");
            cmd.Parameters.AddWithValue("@Subtotal", invoice.Subtotal);
            cmd.Parameters.AddWithValue("@TaxRate", invoice.TaxRate);
            cmd.Parameters.AddWithValue("@TaxAmount", invoice.TaxAmount);
            cmd.Parameters.AddWithValue("@DiscountAmount", invoice.DiscountAmount);
            cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
            cmd.Parameters.AddWithValue("@PaymentStatus", invoice.PaymentStatus ?? "Draft");
            cmd.Parameters.AddWithValue("@Notes", invoice.Notes ?? "");
            cmd.Parameters.AddWithValue("@PdfPath", invoice.PdfPath ?? "");
            cmd.Parameters.AddWithValue("@IsArchived", invoice.IsArchived);
            cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
            cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
        }

        // Helper method to map data reader to Invoice object
        private Invoice MapInvoiceFromReader(SqlDataReader reader)
        {
            return new Invoice
            {
                InvoiceId = Convert.ToInt32(reader["invoice_id"]),
                CustomerId = Convert.ToInt32(reader["customer_id"]),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                InvoiceNumber = reader["invoice_number"]?.ToString() ?? "",
                InvoiceDate = Convert.ToDateTime(reader["invoice_date"]),
                DueDate = Convert.ToDateTime(reader["due_date"]),
                PaymentTerms = reader["payment_terms"]?.ToString() ?? "Net 30",
                Subtotal = Convert.ToDecimal(reader["subtotal"]),
                TaxRate = Convert.ToDecimal(reader["tax_rate"]),
                TaxAmount = Convert.ToDecimal(reader["tax_amount"]),
                DiscountAmount = Convert.ToDecimal(reader["discount_amount"]),
                TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                PaymentStatus = reader["payment_status"]?.ToString() ?? "Draft",
                Notes = reader["notes"]?.ToString() ?? "",
                PdfPath = reader["pdf_path"]?.ToString() ?? "",
                IsArchived = Convert.ToBoolean(reader["is_archived"]),
                ArchivedDate = reader["archived_date"] != DBNull.Value ? Convert.ToDateTime(reader["archived_date"]) : null,
                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                ModifiedDate = Convert.ToDateTime(reader["modified_date"]),
                CustomerName = reader["customer_name"]?.ToString() ?? "",
                CustomerEmail = reader["customer_email"]?.ToString() ?? "",
                CustomerCompany = reader["customer_company"]?.ToString() ?? "",
                CreatedByName = reader["created_by_name"]?.ToString() ?? ""
            };
        }

        // Helper method to map data reader to InvoiceItem object
        private InvoiceItem MapInvoiceItemFromReader(SqlDataReader reader)
        {
            return new InvoiceItem
            {
                ItemId = Convert.ToInt32(reader["item_id"]),
                InvoiceId = Convert.ToInt32(reader["invoice_id"]),
                ItemOrder = Convert.ToInt32(reader["item_order"]),
                Description = reader["description"]?.ToString() ?? "",
                Quantity = Convert.ToDecimal(reader["quantity"]),
                UnitPrice = Convert.ToDecimal(reader["unit_price"]),
                Amount = Convert.ToDecimal(reader["amount"])
            };
        }
    }
}