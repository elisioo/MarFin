using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MarFin_Final.Database.Services
{
    /// <summary>
    /// Diagnostic utility to test connectivity to the remote database
    /// </summary>
    public class ConnectionDiagnostics
    {
        private const string RemoteServer = "db33549.public.databaseasp.net";
        private const int SqlPort = 1433;
        private const string ConnectionString = "Server=db33549.public.databaseasp.net;" +
                                                "Database=db33549;" +
                                                "User Id=db33549;" +
                                                "Password=marfindbit13;" +
                                                "Encrypt=True;" +
                                                "TrustServerCertificate=True;" +
                                                "MultipleActiveResultSets=True;";

        /// <summary>
        /// Test DNS resolution for the server
        /// </summary>
        public static async Task<(bool success, string message)> TestDnsResolutionAsync()
        {
            try
            {
                var ipHostEntry = await Dns.GetHostEntryAsync(RemoteServer);
                if (ipHostEntry.AddressList.Length > 0)
                {
                    var ipAddress = ipHostEntry.AddressList[0];
                    return (true, $"✓ DNS resolved successfully: {RemoteServer} → {ipAddress}");
                }
                return (false, $"✗ DNS resolution failed: No IP addresses found for {RemoteServer}");
            }
            catch (Exception ex)
            {
                return (false, $"✗ DNS resolution error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test TCP connectivity to the SQL Server port
        /// </summary>
        public static async Task<(bool success, string message)> TestTcpConnectivityAsync()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(RemoteServer, SqlPort);
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(5000));

                    if (completedTask == connectTask && client.Connected)
                    {
                        return (true, $"✓ TCP connection successful to {RemoteServer}:{SqlPort}");
                    }
                    else
                    {
                        return (false, $"✗ TCP connection timeout to {RemoteServer}:{SqlPort} (waited 5 seconds)");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"✗ TCP connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test SQL Server database connection
        /// </summary>
        public static async Task<(bool success, string message)> TestSqlConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    var openTask = connection.OpenAsync();
                    var completedTask = await Task.WhenAny(openTask, Task.Delay(10000));

                    if (completedTask == openTask && connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                        return (true, $"✓ SQL Server connection successful");
                    }
                    else
                    {
                        return (false, $"✗ SQL Server connection timeout (waited 10 seconds)");
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return (false, $"✗ SQL Server error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"✗ Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Run all diagnostic tests
        /// </summary>
        public static async Task<string> RunAllDiagnosticsAsync()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("=== Database Connection Diagnostics ===");
            results.AppendLine($"Server: {RemoteServer}");
            results.AppendLine($"Port: {SqlPort}");
            results.AppendLine($"Database: db33549");
            results.AppendLine();

            // Test 1: DNS Resolution
            results.AppendLine("Test 1: DNS Resolution");
            var (dnsSuccess, dnsMessage) = await TestDnsResolutionAsync();
            results.AppendLine(dnsMessage);
            results.AppendLine();

            // Test 2: TCP Connectivity
            results.AppendLine("Test 2: TCP Connectivity");
            var (tcpSuccess, tcpMessage) = await TestTcpConnectivityAsync();
            results.AppendLine(tcpMessage);
            results.AppendLine();

            // Test 3: SQL Connection
            results.AppendLine("Test 3: SQL Server Connection");
            var (sqlSuccess, sqlMessage) = await TestSqlConnectionAsync();
            results.AppendLine(sqlMessage);
            results.AppendLine();

            // Summary
            results.AppendLine("=== Summary ===");
            if (dnsSuccess && tcpSuccess && sqlSuccess)
            {
                results.AppendLine("✓ All tests passed! Connection should work.");
            }
            else if (dnsSuccess && tcpSuccess)
            {
                results.AppendLine("⚠ DNS and TCP work, but SQL connection failed. Check credentials.");
            }
            else if (dnsSuccess)
            {
                results.AppendLine("⚠ DNS works, but TCP connection failed. Check firewall/port 1433.");
            }
            else
            {
                results.AppendLine("✗ DNS resolution failed. Check server address or internet connection.");
            }

            return results.ToString();
        }
    }
}
