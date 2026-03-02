using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MarFin_Final.Database.Services
{
    public class AuditLogEntry
    {
        public long LogId { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string ActivityType { get; set; } = "";
        public string TableName { get; set; } = "";
        public int? RecordId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AuditLogFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? ActivityType { get; set; }
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogResult
    {
        public List<AuditLogEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class AuditLogSummary
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int LoginCount { get; set; }
        public int DataChanges { get; set; }
    }

    public class AuditLogService
    {
        private readonly string _connectionString;

        public AuditLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<AuditLogResult> GetLogsAsync(AuditLogFilter filter)
        {
            var result = new AuditLogResult();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = BuildWhereClause(filter);

            // Count query
            var countSql = $@"
                SELECT COUNT(*)
                FROM tbl_Activity_Log al
                INNER JOIN tbl_Users u ON al.user_id = u.user_id
                INNER JOIN tbl_Roles r ON u.role_id = r.role_id
                {whereClause}";

            using (var countCmd = new SqlCommand(countSql, connection))
            {
                AddParameters(countCmd, filter);
                result.TotalCount = (int)await countCmd.ExecuteScalarAsync();
            }

            result.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)filter.PageSize);

            // Data query with pagination
            var offset = (filter.Page - 1) * filter.PageSize;
            var dataSql = $@"
                SELECT 
                    al.log_id,
                    al.user_id,
                    ISNULL(u.first_name + ' ' + u.last_name, u.email) AS full_name,
                    u.email,
                    r.role_name,
                    al.activity_type,
                    ISNULL(al.table_name, '') AS table_name,
                    al.record_id,
                    al.action,
                    al.old_values,
                    al.new_values,
                    al.ip_address,
                    al.user_agent,
                    al.created_date
                FROM tbl_Activity_Log al
                INNER JOIN tbl_Users u ON al.user_id = u.user_id
                INNER JOIN tbl_Roles r ON u.role_id = r.role_id
                {whereClause}
                ORDER BY al.created_date DESC
                OFFSET {offset} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";

            using var cmd = new SqlCommand(dataSql, connection);
            AddParameters(cmd, filter);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Entries.Add(new AuditLogEntry
                {
                    LogId        = reader.GetInt64(reader.GetOrdinal("log_id")),
                    UserId       = reader.GetInt32(reader.GetOrdinal("user_id")),
                    UserFullName = reader["full_name"].ToString() ?? "",
                    UserEmail    = reader["email"].ToString() ?? "",
                    UserRole     = reader["role_name"].ToString() ?? "",
                    ActivityType = reader["activity_type"].ToString() ?? "",
                    TableName    = reader["table_name"].ToString() ?? "",
                    RecordId     = reader["record_id"] as int?,
                    Action       = reader["action"].ToString() ?? "",
                    OldValues    = reader["old_values"] as string,
                    NewValues    = reader["new_values"] as string,
                    IpAddress    = reader["ip_address"] as string,
                    UserAgent    = reader["user_agent"] as string,
                    CreatedDate  = reader.GetDateTime(reader.GetOrdinal("created_date"))
                });
            }

            return result;
        }

        public async Task<AuditLogSummary> GetSummaryAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT
                    COUNT(*) AS total_logs,
                    SUM(CASE WHEN CAST(created_date AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS today_logs,
                    SUM(CASE WHEN action IN ('LOGIN','LOGOUT') THEN 1 ELSE 0 END) AS login_count,
                    SUM(CASE WHEN action IN ('INSERT','UPDATE','DELETE','ARCHIVE','RESTORE') THEN 1 ELSE 0 END) AS data_changes
                FROM tbl_Activity_Log";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new AuditLogSummary
                {
                    TotalLogs   = reader.GetInt32(reader.GetOrdinal("total_logs")),
                    TodayLogs   = reader.GetInt32(reader.GetOrdinal("today_logs")),
                    LoginCount  = reader.GetInt32(reader.GetOrdinal("login_count")),
                    DataChanges = reader.GetInt32(reader.GetOrdinal("data_changes"))
                };
            }

            return new AuditLogSummary();
        }

        public async Task<List<(int UserId, string FullName)>> GetAllUsersAsync()
        {
            var users = new List<(int, string)>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT user_id, ISNULL(first_name + ' ' + last_name, email) AS full_name
                FROM tbl_Users
                WHERE is_active = 1
                ORDER BY full_name";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add((reader.GetInt32(0), reader["full_name"].ToString() ?? ""));
            }

            return users;
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static string BuildWhereClause(AuditLogFilter filter)
        {
            var conditions = new List<string>();

            if (filter.StartDate.HasValue)
                conditions.Add("al.created_date >= @StartDate");

            if (filter.EndDate.HasValue)
                conditions.Add("al.created_date < @EndDate");

            if (filter.UserId.HasValue && filter.UserId > 0)
                conditions.Add("al.user_id = @UserId");

            if (!string.IsNullOrWhiteSpace(filter.Action) && filter.Action != "All")
                conditions.Add("al.action = @Action");

            if (!string.IsNullOrWhiteSpace(filter.ActivityType) && filter.ActivityType != "All")
                conditions.Add("al.activity_type = @ActivityType");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
                conditions.Add(@"(
                    u.first_name + ' ' + u.last_name LIKE @SearchText
                    OR u.email LIKE @SearchText
                    OR al.activity_type LIKE @SearchText
                    OR al.table_name LIKE @SearchText
                )");

            return conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : "";
        }

        private static void AddParameters(SqlCommand cmd, AuditLogFilter filter)
        {
            if (filter.StartDate.HasValue)
                cmd.Parameters.AddWithValue("@StartDate", filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                cmd.Parameters.AddWithValue("@EndDate", filter.EndDate.Value.Date.AddDays(1));

            if (filter.UserId.HasValue && filter.UserId > 0)
                cmd.Parameters.AddWithValue("@UserId", filter.UserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Action) && filter.Action != "All")
                cmd.Parameters.AddWithValue("@Action", filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.ActivityType) && filter.ActivityType != "All")
                cmd.Parameters.AddWithValue("@ActivityType", filter.ActivityType);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
                cmd.Parameters.AddWithValue("@SearchText", $"%{filter.SearchText}%");
        }
    }
}
