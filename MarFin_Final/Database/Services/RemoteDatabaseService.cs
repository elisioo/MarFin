using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using MarFin_Final.Data;
using MarFin_Final.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MarFin_Final.Database.Services
{
    public class RemoteDatabaseService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private const int DefaultRemoteUserId = 1;
        private const int DefaultRemoteSegmentId = 1;

        public RemoteDatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Prefer CloudConnection from appsettings; fall back to the known remote connection if missing
            _connectionString = _configuration.GetConnectionString("CloudConnection")
                ?? "Server=db33549.public.databaseasp.net;" +
                   "Database=db33549;" +
                   "User Id=db33549;" +
                   "Password=marfindbit13;" +
                   "Encrypt=True;" +
                   "TrustServerCertificate=True;" +
                   "Connection Timeout=30;" +
                   "MultipleActiveResultSets=True;";
        }

        private AppDbContext CreateRemoteDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            return new AppDbContext(options);
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
                        diagnostic.AddStep(tableExists ? "✓tbl_Customers exists" : "tbl_Customers does NOT exist");
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
                                    diagnostic.AddStep("INSERT permission verified (test rolled back)");
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
                        diagnostic.AddStep("Cannot proceed - table does not exist");
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

        public async Task<int> SyncUsersToRemoteAsync(List<User> users)
        {
            int syncedCount = 0;

            if (users == null || users.Count == 0)
            {
                Console.WriteLine("RemoteDatabaseService: No users provided to sync");
                return 0;
            }

            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine($"USER SYNC STARTING: {users.Count} users to process");
            Console.WriteLine("═══════════════════════════════════════════════════");

            using (var context = CreateRemoteDbContext())
            {
                foreach (var user in users)
                {
                    try
                    {
                        var email = user.Email ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            Console.WriteLine("  Skipping user with empty email");
                            continue;
                        }

                        var remoteUser = await context.Users
                            .FirstOrDefaultAsync(u => u.Email == email);

                        if (remoteUser == null)
                        {
                            Console.WriteLine($"  Inserting user: {user.FirstName} {user.LastName} ({user.Email})");

                            remoteUser = new User
                            {
                                RoleId = user.RoleId,
                                Email = user.Email,
                                PasswordHash = user.PasswordHash,
                                Salt = user.Salt,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                Phone = user.Phone,
                                Department = user.Department,
                                ProfileImagePath = user.ProfileImagePath,
                                IsActive = user.IsActive,
                                LastLogin = user.LastLogin,
                                FailedLoginAttempts = user.FailedLoginAttempts,
                                LockedUntil = user.LockedUntil,
                                CreatedDate = user.CreatedDate,
                                ModifiedDate = user.ModifiedDate
                            };

                            await context.Users.AddAsync(remoteUser);
                        }
                        else
                        {
                            Console.WriteLine($"  Updating user: {user.FirstName} {user.LastName} ({user.Email})");

                            remoteUser.RoleId = user.RoleId;
                            remoteUser.FirstName = user.FirstName;
                            remoteUser.LastName = user.LastName;
                            remoteUser.Phone = user.Phone;
                            remoteUser.Department = user.Department;
                            remoteUser.ProfileImagePath = user.ProfileImagePath;
                            remoteUser.IsActive = user.IsActive;
                            remoteUser.LastLogin = user.LastLogin;
                            remoteUser.FailedLoginAttempts = user.FailedLoginAttempts;
                            remoteUser.LockedUntil = user.LockedUntil;
                            remoteUser.ModifiedDate = user.ModifiedDate;
                        }

                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ✗ Error syncing user {user.Email}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"      Inner: {ex.InnerException.Message}");
                        }
                    }
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine("✗ DbUpdateException during user sync SaveChangesAsync:");
                    Console.WriteLine($"  Error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"  Inner: {dbEx.InnerException.Message}");
                    }

                    foreach (var entry in dbEx.Entries)
                    {
                        Console.WriteLine($"  Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    }

                    throw;
                }
            }

            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine($"USER SYNC COMPLETE: {syncedCount} of {users.Count} users synced");
            Console.WriteLine("═══════════════════════════════════════════════════");

            return syncedCount;
        }

        public async Task<List<User>> GetAllRemoteUsersAsync()
        {
            using (var context = CreateRemoteDbContext())
            {
                return await context.Users.AsNoTracking().ToListAsync();
            }
        }

        // IMPROVED: Sync customers with detailed logging using Entity Framework
        public async Task<int> SyncCustomersToRemoteAsync(List<Customer> customers)
        {
            int syncedCount = 0;

            if (customers == null || customers.Count == 0)
            {
                Console.WriteLine("RemoteDatabaseService: No customers provided to sync");
                return 0;
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"SYNC STARTING: {customers.Count} customers to process");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            using (var context = CreateRemoteDbContext())
            {
                foreach (var customer in customers)
                {
                    try
                    {
                        var email = customer.Email ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            Console.WriteLine("  Skipping customer with empty email");
                            continue;
                        }

                        // Find existing customer by email
                        var existingCustomer = await context.Customers
                            .FirstOrDefaultAsync(c => c.Email == email);

                        if (existingCustomer == null)
                        {
                            Console.WriteLine($"  Inserting: {customer.FirstName} {customer.LastName} ({customer.Email})");

                            var newCustomer = new Customer
                            {
                                SegmentId = DefaultRemoteSegmentId,
                                CreatedBy = DefaultRemoteUserId,

                                ModifiedBy = customer.ModifiedBy,
                                FirstName = customer.FirstName,
                                LastName = customer.LastName,
                                Email = customer.Email,
                                Phone = customer.Phone,
                                CompanyName = customer.CompanyName,
                                Address = customer.Address,
                                City = customer.City,
                                StateProvince = customer.StateProvince,
                                PostalCode = customer.PostalCode,
                                Country = customer.Country,
                                CustomerStatus = customer.CustomerStatus,
                                TotalRevenue = customer.TotalRevenue,
                                Source = customer.Source,
                                Notes = customer.Notes,
                                IsActive = customer.IsActive,
                                IsArchived = customer.IsArchived,
                                ArchivedDate = customer.ArchivedDate,
                                ArchivedBy = customer.ArchivedBy,
                                CreatedDate = customer.CreatedDate,
                                ModifiedDate = customer.ModifiedDate
                            };

                            await context.Customers.AddAsync(newCustomer);
                        }
                        else
                        {
                            Console.WriteLine($"  Updating: {customer.FirstName} {customer.LastName} (Email: {customer.Email})");

                            // Ensure foreign keys point to valid remote records
                            existingCustomer.SegmentId = DefaultRemoteSegmentId;
                            existingCustomer.CreatedBy = DefaultRemoteUserId;

                            existingCustomer.FirstName = customer.FirstName;
                            existingCustomer.LastName = customer.LastName;
                            existingCustomer.Phone = customer.Phone;
                            existingCustomer.CompanyName = customer.CompanyName;
                            existingCustomer.Address = customer.Address;
                            existingCustomer.City = customer.City;
                            existingCustomer.StateProvince = customer.StateProvince;
                            existingCustomer.PostalCode = customer.PostalCode;
                            existingCustomer.Country = customer.Country;
                            existingCustomer.CustomerStatus = customer.CustomerStatus;
                            existingCustomer.TotalRevenue = customer.TotalRevenue;
                            existingCustomer.Source = customer.Source;
                            existingCustomer.Notes = customer.Notes;
                            existingCustomer.IsActive = customer.IsActive;
                            existingCustomer.IsArchived = customer.IsArchived;
                            existingCustomer.ArchivedDate = customer.ArchivedDate;
                            existingCustomer.ArchivedBy = customer.ArchivedBy;
                            existingCustomer.ModifiedDate = customer.ModifiedDate;
                        }

                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ✗ Error syncing customer {customer.Email}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"      Inner: {ex.InnerException.Message}");
                        }
                    }
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine("✗ DbUpdateException during customer sync SaveChangesAsync:");
                    Console.WriteLine($"  Error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"  Inner: {dbEx.InnerException.Message}");
                    }

                    foreach (var entry in dbEx.Entries)
                    {
                        Console.WriteLine($"  Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    }

                    throw;
                }
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"SYNC COMPLETE: {syncedCount} of {customers.Count} customers synced");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            return syncedCount;
        }

        public async Task<int> SyncInvoicesToRemoteAsync(List<Invoice> invoices)
        {
            int syncedCount = 0;

            if (invoices == null || invoices.Count == 0)
            {
                Console.WriteLine("RemoteDatabaseService: No invoices provided to sync");
                return 0;
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"INVOICE SYNC STARTING: {invoices.Count} invoices to process");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            using (var context = CreateRemoteDbContext())
            {
                foreach (var invoice in invoices)
                {
                    try
                    {
                        var email = invoice.CustomerEmail;
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            Console.WriteLine($"  ✗ Skipping invoice {invoice.InvoiceNumber}: no customer email available");
                            continue;
                        }

                        var remoteCustomer = await context.Customers
                            .FirstOrDefaultAsync(c => c.Email == email);

                        if (remoteCustomer == null)
                        {
                            Console.WriteLine($"  ✗ Skipping invoice {invoice.InvoiceNumber}: no matching remote customer for email '{email}'");
                            continue;
                        }

                        var remoteInvoice = await context.Invoices
                            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);

                        if (remoteInvoice == null)
                        {
                            Console.WriteLine($"  Inserting invoice: {invoice.InvoiceNumber}");

                            remoteInvoice = new Invoice
                            {
                                CustomerId = remoteCustomer.CustomerId,
                                CreatedBy = DefaultRemoteUserId,

                                InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                                InvoiceDate = invoice.InvoiceDate,
                                DueDate = invoice.DueDate,
                                PaymentTerms = invoice.PaymentTerms ?? "Net 30",
                                Subtotal = invoice.Subtotal,
                                TaxRate = invoice.TaxRate,
                                TaxAmount = invoice.TaxAmount,
                                DiscountAmount = invoice.DiscountAmount,
                                TotalAmount = invoice.TotalAmount,
                                PaymentStatus = invoice.PaymentStatus ?? "Draft",
                                Notes = invoice.Notes,
                                PdfPath = invoice.PdfPath,
                                IsArchived = invoice.IsArchived,
                                ArchivedDate = invoice.ArchivedDate,
                                CreatedDate = invoice.CreatedDate,
                                ModifiedDate = invoice.ModifiedDate
                            };

                            await context.Invoices.AddAsync(remoteInvoice);
                        }
                        else
                        {
                            Console.WriteLine($"  Updating invoice: {invoice.InvoiceNumber}");

                            remoteInvoice.CustomerId = remoteCustomer.CustomerId;
                            remoteInvoice.CreatedBy = DefaultRemoteUserId;

                            remoteInvoice.InvoiceDate = invoice.InvoiceDate;
                            remoteInvoice.DueDate = invoice.DueDate;
                            remoteInvoice.PaymentTerms = invoice.PaymentTerms ?? "Net 30";
                            remoteInvoice.Subtotal = invoice.Subtotal;
                            remoteInvoice.TaxRate = invoice.TaxRate;
                            remoteInvoice.TaxAmount = invoice.TaxAmount;
                            remoteInvoice.DiscountAmount = invoice.DiscountAmount;
                            remoteInvoice.TotalAmount = invoice.TotalAmount;
                            remoteInvoice.PaymentStatus = invoice.PaymentStatus ?? "Draft";
                            remoteInvoice.Notes = invoice.Notes;
                            remoteInvoice.PdfPath = invoice.PdfPath;
                            remoteInvoice.IsArchived = invoice.IsArchived;
                            remoteInvoice.ArchivedDate = invoice.ArchivedDate;
                            remoteInvoice.ModifiedDate = invoice.ModifiedDate;
                        }

                        syncedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ✗ Error syncing invoice {invoice.InvoiceNumber}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"      Inner: {ex.InnerException.Message}");
                        }
                    }
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine("✗ DbUpdateException during invoice sync SaveChangesAsync:");
                    Console.WriteLine($"  Error: {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"  Inner: {dbEx.InnerException.Message}");
                    }

                    foreach (var entry in dbEx.Entries)
                    {
                        Console.WriteLine($"  Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                    }

                    throw;
                }
            }

            Console.WriteLine($"═══════════════════════════════════════════════════");
            Console.WriteLine($"INVOICE SYNC COMPLETE: {syncedCount} of {invoices.Count} invoices synced");
            Console.WriteLine($"═══════════════════════════════════════════════════");

            return syncedCount;
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
                    Console.WriteLine("WARNING: No customers provided to sync!");
                    Console.WriteLine("   → Check if local database has any active, non-archived customers");
                }

                // Sync Invoices (if needed - reuse similar logic)
                if (invoices != null && invoices.Count > 0)
                {
                    result.InvoicesAttempted = invoices.Count;
                    result.InvoicesSynced = await SyncInvoicesToRemoteAsync(invoices);
                }

                result.TotalSynced = result.CustomersSynced + result.InvoicesSynced;
                result.IsSuccess = result.TotalSynced > 0;

                if (result.TotalSynced > 0)
                {
                    result.Message = $" Sync completed: {result.CustomersSynced} customers and {result.InvoicesSynced} invoices synced successfully";
                }
                else
                {
                    result.Message = "No records were synced. Run diagnostics to investigate.";
                }
            }
            catch (DbUpdateException dbEx)
            {
                result.IsSuccess = false;

                var innerMessage = dbEx.InnerException?.Message;
                result.Message = innerMessage != null
                    ? $"Database update error: {dbEx.Message} | Inner: {innerMessage}"
                    : $"Database update error: {dbEx.Message}";

                Console.WriteLine(result.Message);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;

                var innerMessage = ex.InnerException?.Message;
                result.Message = innerMessage != null
                    ? $"Sync error: {ex.Message} | Inner: {innerMessage}"
                    : $"Sync error: {ex.Message}";

                Console.WriteLine($"SYNC ERROR: {result.Message}");
            }

            return result;
        }

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
            catch
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