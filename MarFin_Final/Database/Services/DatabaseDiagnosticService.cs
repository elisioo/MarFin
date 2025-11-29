// Services/DatabaseDiagnosticService.cs
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MarFin_Final.Database.Services
{
    public class DiagnosticResult
    {
        public bool Success { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public DateTime TestTime { get; set; } = DateTime.Now;
    }

    public class DatabaseDiagnosticService
    {
        private readonly string _connectionString;

        public DatabaseDiagnosticService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<DiagnosticResult>> RunFullDiagnosticAsync()
        {
            var results = new List<DiagnosticResult>();

            // Test 1: Connection String Validation
            results.Add(TestConnectionString());

            // Test 2: Database Connection
            results.Add(await TestDatabaseConnectionAsync());

            // Test 3: Check if tables exist
            results.Add(await TestTablesExistAsync());

            // Test 4: Test SELECT permission
            results.Add(await TestSelectPermissionAsync());

            // Test 5: Test INSERT permission
            results.Add(await TestInsertPermissionAsync());

            // Test 6: Test UPDATE permission
            results.Add(await TestUpdatePermissionAsync());

            // Test 7: Test stored procedures
            results.Add(await TestStoredProceduresAsync());

            return results;
        }

        private DiagnosticResult TestConnectionString()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    return new DiagnosticResult
                    {
                        Success = false,
                        TestName = "Connection String Validation",
                        Message = "Connection string is null or empty",
                        ErrorDetails = "No connection string provided"
                    };
                }

                var builder = new SqlConnectionStringBuilder(_connectionString);

                var details = $"Server: {builder.DataSource}\n" +
                             $"Database: {builder.InitialCatalog}\n" +
                             $"User ID: {builder.UserID}\n" +
                             $"Integrated Security: {builder.IntegratedSecurity}";

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "Connection String Validation",
                    Message = "Connection string is valid",
                    ErrorDetails = details
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "Connection String Validation",
                    Message = "Invalid connection string format",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<DiagnosticResult> TestDatabaseConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "Database Connection",
                    Message = $"Successfully connected to database: {connection.Database}",
                    ErrorDetails = $"Server Version: {connection.ServerVersion}"
                };
            }
            catch (SqlException ex)
            {
                var errorMessage = ex.Number switch
                {
                    -1 => "Connection timeout - The server may be unavailable or unreachable",
                    -2 => "Timeout expired - Check if SQL Server is running",
                    2 => "Network error - Cannot find server",
                    18456 => "Login failed - Check username and password",
                    4060 => "Cannot open database - Database may not exist",
                    _ => $"SQL Error {ex.Number}: {ex.Message}"
                };

                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "Database Connection",
                    Message = errorMessage,
                    ErrorDetails = $"Error Number: {ex.Number}\n{ex.ToString()}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "Database Connection",
                    Message = "Failed to connect to database",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        private async Task<DiagnosticResult> TestTablesExistAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE' 
                    AND TABLE_NAME IN ('tbl_Invoices', 'tbl_Customers', 'tbl_Invoice_Items', 'tbl_Users')
                    ORDER BY TABLE_NAME";

                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                var tables = new List<string>();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                var expectedTables = new[] { "tbl_Customers", "tbl_Invoice_Items", "tbl_Invoices", "tbl_Users" };
                var missingTables = new List<string>();

                foreach (var table in expectedTables)
                {
                    if (!tables.Contains(table))
                    {
                        missingTables.Add(table);
                    }
                }

                if (missingTables.Count > 0)
                {
                    return new DiagnosticResult
                    {
                        Success = false,
                        TestName = "Table Existence Check",
                        Message = $"Missing tables: {string.Join(", ", missingTables)}",
                        ErrorDetails = $"Found tables: {string.Join(", ", tables)}"
                    };
                }

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "Table Existence Check",
                    Message = "All required tables exist",
                    ErrorDetails = $"Found tables: {string.Join(", ", tables)}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "Table Existence Check",
                    Message = "Failed to check table existence",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        private async Task<DiagnosticResult> TestSelectPermissionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT TOP 1 invoice_id FROM tbl_Invoices";
                using var cmd = new SqlCommand(query, connection);
                await cmd.ExecuteScalarAsync();

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "SELECT Permission",
                    Message = "User has SELECT permission on tbl_Invoices"
                };
            }
            catch (SqlException ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "SELECT Permission",
                    Message = "User does not have SELECT permission",
                    ErrorDetails = $"Error {ex.Number}: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "SELECT Permission",
                    Message = "Failed to test SELECT permission",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        private async Task<DiagnosticResult> TestInsertPermissionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Test with a dry-run transaction that will be rolled back
                using var transaction = connection.BeginTransaction();

                try
                {
                    var query = @"
                        INSERT INTO tbl_Invoices (
                            customer_id, created_by, invoice_number, invoice_date, due_date,
                            payment_terms, subtotal, tax_rate, tax_amount, discount_amount,
                            total_amount, payment_status
                        )
                        VALUES (
                            1, 1, 'TEST-DIAGNOSTIC', GETDATE(), GETDATE(),
                            'Test', 0, 0, 0, 0, 0, 'Draft'
                        )";

                    using var cmd = new SqlCommand(query, connection, transaction);
                    await cmd.ExecuteNonQueryAsync();

                    transaction.Rollback(); // Always rollback test data

                    return new DiagnosticResult
                    {
                        Success = true,
                        TestName = "INSERT Permission",
                        Message = "User has INSERT permission on tbl_Invoices"
                    };
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();

                    if (ex.Number == 547) // Foreign key violation
                    {
                        return new DiagnosticResult
                        {
                            Success = true,
                            TestName = "INSERT Permission",
                            Message = "User has INSERT permission (foreign key constraint detected)",
                            ErrorDetails = "Permission exists but test data violated foreign key constraints"
                        };
                    }

                    throw;
                }
            }
            catch (SqlException ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "INSERT Permission",
                    Message = "User does not have INSERT permission",
                    ErrorDetails = $"Error {ex.Number}: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "INSERT Permission",
                    Message = "Failed to test INSERT permission",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        private async Task<DiagnosticResult> TestUpdatePermissionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // This will succeed or fail based on permissions, not based on whether records exist
                var query = "UPDATE tbl_Invoices SET modified_date = GETDATE() WHERE invoice_id = -1";
                using var cmd = new SqlCommand(query, connection);
                await cmd.ExecuteNonQueryAsync();

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "UPDATE Permission",
                    Message = "User has UPDATE permission on tbl_Invoices"
                };
            }
            catch (SqlException ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "UPDATE Permission",
                    Message = "User does not have UPDATE permission",
                    ErrorDetails = $"Error {ex.Number}: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "UPDATE Permission",
                    Message = "Failed to test UPDATE permission",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        private async Task<DiagnosticResult> TestStoredProceduresAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT ROUTINE_NAME 
                    FROM INFORMATION_SCHEMA.ROUTINES 
                    WHERE ROUTINE_TYPE = 'PROCEDURE'
                    AND ROUTINE_NAME LIKE 'sp_%'
                    ORDER BY ROUTINE_NAME";

                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                var procedures = new List<string>();
                while (await reader.ReadAsync())
                {
                    procedures.Add(reader.GetString(0));
                }

                return new DiagnosticResult
                {
                    Success = true,
                    TestName = "Stored Procedures Check",
                    Message = $"Found {procedures.Count} stored procedures",
                    ErrorDetails = string.Join("\n", procedures)
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Success = false,
                    TestName = "Stored Procedures Check",
                    Message = "Failed to check stored procedures",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        public async Task<string> GetDetailedConnectionInfoAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        @@SERVERNAME AS ServerName,
                        DB_NAME() AS DatabaseName,
                        SUSER_SNAME() AS CurrentUser,
                        @@VERSION AS SQLVersion,
                        (SELECT COUNT(*) FROM tbl_Invoices) AS InvoiceCount,
                        (SELECT COUNT(*) FROM tbl_Customers) AS CustomerCount";

                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return $"Server: {reader["ServerName"]}\n" +
                           $"Database: {reader["DatabaseName"]}\n" +
                           $"User: {reader["CurrentUser"]}\n" +
                           $"SQL Version: {reader["SQLVersion"]}\n" +
                           $"Invoices: {reader["InvoiceCount"]}\n" +
                           $"Customers: {reader["CustomerCount"]}";
                }

                return "Unable to retrieve connection information";
            }
            catch (Exception ex)
            {
                return $"Error getting connection info: {ex.Message}";
            }
        }
    }
}