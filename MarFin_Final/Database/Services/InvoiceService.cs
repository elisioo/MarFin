// Services/InvoiceService.cs
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MarFin.Models;

namespace MarFin.Services
{
    public class ServiceException : Exception
    {
        public string ErrorCode { get; set; }
        public string UserFriendlyMessage { get; set; }

        public ServiceException(string message, string userFriendlyMessage, string errorCode = "GENERAL_ERROR")
            : base(message)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode;
        }

        public ServiceException(string message, Exception innerException, string userFriendlyMessage, string errorCode = "GENERAL_ERROR")
            : base(message, innerException)
        {
            UserFriendlyMessage = userFriendlyMessage;
            ErrorCode = errorCode;
        }
    }

    public interface IInvoiceService
    {
        Task<InvoiceListResponse> GetInvoicesAsync(InvoiceFilterRequest filter);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
        Task<int> CreateInvoiceAsync(CreateInvoiceRequest request, int userId);
        Task<bool> UpdateInvoiceStatusAsync(UpdateInvoiceStatusRequest request);
        Task<bool> DeleteInvoiceAsync(int invoiceId, int userId);
        Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<string> GenerateInvoiceNumberAsync();
        Task<bool> TestConnectionAsync();
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly string _connectionString;

        public InvoiceService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            }
            _connectionString = connectionString;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Database connection failed: {ex.Message}",
                    "Unable to connect to the database. Please check your connection settings.",
                    "DB_CONNECTION_ERROR"
                );
            }
        }

        public async Task<InvoiceListResponse> GetInvoicesAsync(InvoiceFilterRequest filter)
        {
            var response = new InvoiceListResponse
            {
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get total count first
                var countQuery = @"
                    SELECT COUNT(*)
                    FROM tbl_Invoices i
                    INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                    WHERE i.is_archived = 0
                        AND (@SearchTerm IS NULL OR 
                             i.invoice_number LIKE '%' + @SearchTerm + '%' OR
                             c.first_name LIKE '%' + @SearchTerm + '%' OR
                             c.last_name LIKE '%' + @SearchTerm + '%' OR
                             c.company_name LIKE '%' + @SearchTerm + '%')
                        AND (@PaymentStatus IS NULL OR i.payment_status = @PaymentStatus)
                        AND (@StartDate IS NULL OR i.invoice_date >= @StartDate)
                        AND (@EndDate IS NULL OR i.invoice_date <= @EndDate)";

                using (var countCmd = new SqlCommand(countQuery, connection))
                {
                    countCmd.Parameters.AddWithValue("@SearchTerm", (object?)filter.SearchTerm ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@PaymentStatus", (object?)filter.PaymentStatus ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@StartDate", (object?)filter.StartDate ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@EndDate", (object?)filter.EndDate ?? DBNull.Value);

                    response.TotalCount = (int)await countCmd.ExecuteScalarAsync();
                }

                // Apply max records limit
                var maxRecords = filter.MaxRecords ?? 200;
                var actualMaxRecords = Math.Min(response.TotalCount, maxRecords);
                response.HasMore = response.TotalCount > maxRecords;

                // Calculate pages based on limited records
                response.TotalPages = (int)Math.Ceiling((double)actualMaxRecords / filter.PageSize);

                // Get paginated data
                var sortColumn = filter.SortBy?.ToLower() switch
                {
                    "invoice_number" => "i.invoice_number",
                    "customer" => "c.company_name",
                    "amount" => "i.total_amount",
                    "invoice_date" => "i.invoice_date",
                    _ => "i.invoice_date"
                };

                var sortOrder = filter.SortOrder?.ToLower() == "asc" ? "ASC" : "DESC";
                var offset = (filter.Page - 1) * filter.PageSize;

                var query = $@"
                    SELECT 
                        i.invoice_id,
                        i.invoice_number,
                        c.first_name + ' ' + c.last_name AS customer_name,
                        ISNULL(c.company_name, '') AS customer_company,
                        i.total_amount,
                        i.invoice_date,
                        i.due_date,
                        i.payment_status,
                        CASE 
                            WHEN i.payment_status IN ('Issued', 'Partial') AND i.due_date < GETDATE() 
                            THEN 1 
                            ELSE 0 
                        END AS is_overdue
                    FROM (
                        SELECT TOP {maxRecords} *
                        FROM tbl_Invoices
                        WHERE is_archived = 0
                            AND (@SearchTerm IS NULL OR invoice_number LIKE '%' + @SearchTerm + '%')
                            AND (@PaymentStatus IS NULL OR payment_status = @PaymentStatus)
                            AND (@StartDate IS NULL OR invoice_date >= @StartDate)
                            AND (@EndDate IS NULL OR invoice_date <= @EndDate)
                    ) i
                    INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                    WHERE (@SearchTerm IS NULL OR 
                           c.first_name LIKE '%' + @SearchTerm + '%' OR
                           c.last_name LIKE '%' + @SearchTerm + '%' OR
                           c.company_name LIKE '%' + @SearchTerm + '%')
                    ORDER BY {sortColumn} {sortOrder}
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", (object?)filter.SearchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentStatus", (object?)filter.PaymentStatus ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StartDate", (object?)filter.StartDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", (object?)filter.EndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        response.Invoices.Add(new InvoiceListItem
                        {
                            InvoiceId = reader.GetInt32(0),
                            InvoiceNumber = reader.GetString(1),
                            CustomerName = reader.GetString(2),
                            CustomerCompany = reader.GetString(3),
                            TotalAmount = reader.GetDecimal(4),
                            InvoiceDate = reader.GetDateTime(5),
                            DueDate = reader.GetDateTime(6),
                            PaymentStatus = reader.GetString(7),
                            IsOverdue = reader.GetInt32(8) == 1
                        });
                    }
                }

                // Get summary
                response.Summary = await GetFinancialSummaryAsync(filter.StartDate, filter.EndDate);

                return response;
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while retrieving invoices: {ex.Message}",
                    "An error occurred while loading invoices. Please try again.",
                    "DB_QUERY_ERROR"
                );
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error retrieving invoices: {ex.Message}",
                    "An unexpected error occurred. Please try again.",
                    "GENERAL_ERROR"
                );
            }
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        i.*,
                        c.first_name + ' ' + c.last_name AS customer_name,
                        ISNULL(c.company_name, '') AS customer_company
                    FROM tbl_Invoices i
                    INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                    WHERE i.invoice_id = @InvoiceId AND i.is_archived = 0";

                Invoice? invoice = null;

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        invoice = new Invoice
                        {
                            InvoiceId = reader.GetInt32(reader.GetOrdinal("invoice_id")),
                            CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                            CreatedBy = reader.GetInt32(reader.GetOrdinal("created_by")),
                            InvoiceNumber = reader.GetString(reader.GetOrdinal("invoice_number")),
                            InvoiceDate = reader.GetDateTime(reader.GetOrdinal("invoice_date")),
                            DueDate = reader.GetDateTime(reader.GetOrdinal("due_date")),
                            PaymentTerms = reader.IsDBNull(reader.GetOrdinal("payment_terms")) ? null : reader.GetString(reader.GetOrdinal("payment_terms")),
                            Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                            TaxRate = reader.GetDecimal(reader.GetOrdinal("tax_rate")),
                            TaxAmount = reader.GetDecimal(reader.GetOrdinal("tax_amount")),
                            DiscountAmount = reader.IsDBNull(reader.GetOrdinal("discount_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                            TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                            PaymentStatus = reader.IsDBNull(reader.GetOrdinal("payment_status")) ? null : reader.GetString(reader.GetOrdinal("payment_status")),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                            CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                            CustomerCompany = reader.GetString(reader.GetOrdinal("customer_company"))
                        };
                    }
                }

                if (invoice != null)
                {
                    // Get invoice items
                    var itemsQuery = @"
                        SELECT * FROM tbl_Invoice_Items
                        WHERE invoice_id = @InvoiceId
                        ORDER BY item_order";

                    using var itemsCmd = new SqlCommand(itemsQuery, connection);
                    itemsCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    using var itemsReader = await itemsCmd.ExecuteReaderAsync();
                    while (await itemsReader.ReadAsync())
                    {
                        invoice.Items.Add(new InvoiceItem
                        {
                            ItemId = itemsReader.GetInt32(0),
                            InvoiceId = itemsReader.GetInt32(1),
                            ItemOrder = itemsReader.GetInt32(2),
                            Description = itemsReader.GetString(3),
                            Quantity = itemsReader.GetDecimal(4),
                            UnitPrice = itemsReader.GetDecimal(5),
                            Amount = itemsReader.GetDecimal(6)
                        });
                    }
                }

                return invoice;
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while retrieving invoice: {ex.Message}",
                    $"Unable to load invoice #{invoiceId}. Please try again.",
                    "DB_QUERY_ERROR"
                );
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error retrieving invoice: {ex.Message}",
                    "An unexpected error occurred while loading the invoice.",
                    "GENERAL_ERROR"
                );
            }
        }

        public async Task<int> CreateInvoiceAsync(CreateInvoiceRequest request, int userId)
        {
            // === VALIDATION ===
            if (request.CustomerId <= 0)
                throw new ServiceException("Invalid customer", "Please select a customer.", "VALIDATION_ERROR");

            if (request.Items?.Any() != true || request.Items.All(i => string.IsNullOrWhiteSpace(i.Description)))
                throw new ServiceException("No valid items", "Please add at least one item with a description.", "VALIDATION_ERROR");

            if (request.DueDate < request.InvoiceDate)
                throw new ServiceException("Invalid dates", "Due date must be on or after invoice date.", "VALIDATION_ERROR");

            SqlConnection? connection = null;
            SqlTransaction? transaction = null;

            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                // === SAFE CALCULATIONS (NO NULLS EVER) ===
                var subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                var taxAmount = subtotal * (request.TaxRate / 100m);
                var discount = request.DiscountAmount; // Already 0m from model default
                var totalAmount = subtotal + taxAmount - discount;

                // === INSERT MAIN INVOICE ===
                var invoiceQuery = @"
            INSERT INTO tbl_Invoices (
                customer_id, created_by, invoice_number, invoice_date, due_date,
                payment_terms, subtotal, tax_rate, tax_amount, discount_amount,
                total_amount, payment_status, notes, created_date, modified_date
            ) VALUES (
                @CustomerId, @CreatedBy, @InvoiceNumber, @InvoiceDate, @DueDate,
                @PaymentTerms, @Subtotal, @TaxRate, @TaxAmount, @DiscountAmount,
                @TotalAmount, 'Draft', @Notes, GETDATE(), GETDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);";

                int invoiceId;
                using (var cmd = new SqlCommand(invoiceQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@CustomerId", request.CustomerId);
                    cmd.Parameters.AddWithValue("@CreatedBy", userId);
                    cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                    cmd.Parameters.AddWithValue("@InvoiceDate", request.InvoiceDate.Date);
                    cmd.Parameters.AddWithValue("@DueDate", request.DueDate.Date);
                    cmd.Parameters.AddWithValue("@PaymentTerms", (object?)request.PaymentTerms ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                    cmd.Parameters.AddWithValue("@TaxRate", request.TaxRate);
                    cmd.Parameters.AddWithValue("@TaxAmount", taxAmount);
                    cmd.Parameters.AddWithValue("@DiscountAmount", discount);
                    cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    invoiceId = result is DBNull or null
                        ? throw new ServiceException("No ID returned", "Failed to create invoice.", "DB_INSERT_ERROR")
                        : Convert.ToInt32(result);
                }

                // === INSERT LINE ITEMS ===
                for (int i = 0; i < request.Items.Count; i++)
                {
                    var item = request.Items[i];
                    if (string.IsNullOrWhiteSpace(item.Description))
                        throw new ServiceException("Empty description", $"Item #{i + 1} has no description.", "VALIDATION_ERROR");

                    var itemAmount = item.Quantity * item.UnitPrice;

                    var itemQuery = @"
                INSERT INTO tbl_Invoice_Items (invoice_id, item_order, description, quantity, unit_price, amount)
                VALUES (@InvoiceId, @ItemOrder, @Description, @Quantity, @UnitPrice, @Amount);";

                    using var itemCmd = new SqlCommand(itemQuery, connection, transaction);
                    itemCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    itemCmd.Parameters.AddWithValue("@ItemOrder", i + 1);
                    itemCmd.Parameters.AddWithValue("@Description", item.Description.Trim());
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@Amount", itemAmount);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return invoiceId;
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                transaction?.Dispose();
                connection?.Dispose();
            }
        }
        public async Task<bool> UpdateInvoiceStatusAsync(UpdateInvoiceStatusRequest request)
        {
            if (request.InvoiceId <= 0)
            {
                throw new ServiceException(
                    "Invalid invoice ID",
                    "Invalid invoice ID provided.",
                    "VALIDATION_ERROR"
                );
            }

            if (string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                throw new ServiceException(
                    "Payment status is required",
                    "Please provide a valid payment status.",
                    "VALIDATION_ERROR"
                );
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE tbl_Invoices
                    SET payment_status = @PaymentStatus,
                        modified_date = GETDATE()
                    WHERE invoice_id = @InvoiceId AND is_archived = 0";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@InvoiceId", request.InvoiceId);
                cmd.Parameters.AddWithValue("@PaymentStatus", request.PaymentStatus);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new ServiceException(
                        $"No invoice found with ID {request.InvoiceId}",
                        "Invoice not found or already archived.",
                        "NOT_FOUND"
                    );
                }

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while updating invoice status: {ex.Message}",
                    "Failed to update invoice status. Please try again.",
                    "DB_UPDATE_ERROR"
                );
            }
            catch (ServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error updating invoice status: {ex.Message}",
                    "An unexpected error occurred while updating the invoice.",
                    "GENERAL_ERROR"
                );
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int invoiceId, int userId)
        {
            if (invoiceId <= 0)
            {
                throw new ServiceException(
                    "Invalid invoice ID",
                    "Invalid invoice ID provided.",
                    "VALIDATION_ERROR"
                );
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE tbl_Invoices
                    SET is_archived = 1,
                        archived_date = GETDATE(),
                        modified_date = GETDATE()
                    WHERE invoice_id = @InvoiceId AND is_archived = 0";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new ServiceException(
                        $"No invoice found with ID {invoiceId}",
                        "Invoice not found or already archived.",
                        "NOT_FOUND"
                    );
                }

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while deleting invoice: {ex.Message}",
                    "Failed to delete invoice. Please try again.",
                    "DB_DELETE_ERROR"
                );
            }
            catch (ServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error deleting invoice: {ex.Message}",
                    "An unexpected error occurred while deleting the invoice.",
                    "GENERAL_ERROR"
                );
            }
        }

        public async Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        SUM(CASE WHEN payment_status = 'Paid' THEN total_amount ELSE 0 END) AS total_revenue,
                        SUM(CASE WHEN payment_status IN ('Issued', 'Partial') THEN total_amount ELSE 0 END) AS total_pending,
                        SUM(CASE WHEN payment_status IN ('Issued', 'Partial') AND due_date < GETDATE() THEN total_amount ELSE 0 END) AS total_overdue,
                        COUNT(CASE WHEN payment_status IN ('Issued', 'Partial') THEN 1 END) AS pending_count,
                        COUNT(CASE WHEN payment_status IN ('Issued', 'Partial') AND due_date < GETDATE() THEN 1 END) AS overdue_count
                    FROM tbl_Invoices
                    WHERE is_archived = 0
                        AND (@StartDate IS NULL OR invoice_date >= @StartDate)
                        AND (@EndDate IS NULL OR invoice_date <= @EndDate)";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new FinancialSummary
                    {
                        TotalRevenue = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0),
                        TotalPending = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                        TotalOverdue = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        PendingInvoicesCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        OverdueInvoicesCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        RevenueChangePercent = 12.5m // Calculate from previous period
                    };
                }

                return new FinancialSummary();
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while retrieving financial summary: {ex.Message}",
                    "Failed to load financial summary. Please try again.",
                    "DB_QUERY_ERROR"
                );
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error retrieving financial summary: {ex.Message}",
                    "An unexpected error occurred while loading the summary.",
                    "GENERAL_ERROR"
                );
            }
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT TOP 1 invoice_number
                    FROM tbl_Invoices
                    WHERE invoice_number LIKE 'INV-' + CAST(YEAR(GETDATE()) AS VARCHAR(4)) + '-%'
                    ORDER BY invoice_id DESC";

                using var cmd = new SqlCommand(query, connection);
                var result = await cmd.ExecuteScalarAsync();

                if (result != null)
                {
                    var lastNumber = result.ToString()!;
                    var parts = lastNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                    {
                        return $"INV-{DateTime.Now.Year}-{(num + 1):D3}";
                    }
                }

                return $"INV-{DateTime.Now.Year}-001";
            }
            catch (SqlException ex)
            {
                throw new ServiceException(
                    $"Database error while generating invoice number: {ex.Message}",
                    "Failed to generate invoice number. Please try again.",
                    "DB_QUERY_ERROR"
                );
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    $"Error generating invoice number: {ex.Message}",
                    "An unexpected error occurred while generating the invoice number.",
                    "GENERAL_ERROR"
                );
            }
        }
    }
}