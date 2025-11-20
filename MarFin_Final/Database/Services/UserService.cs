using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MarFin_Final.Models;
using System.Security.Cryptography;
using System.Text;
using MarFin_Final.Database.Services;

namespace MarFin_Final.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        private readonly AuthService _authService;

        public UserService(IConfiguration configuration, AuthService authService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");
            _authService = authService;
        }

        // Get all users with their roles
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = new List<UserDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        u.user_id,
                        u.first_name,
                        u.last_name,
                        u.email,
                        u.phone,
                        u.department,
                        u.is_active,
                        u.last_login,
                        r.role_id,
                        r.role_name
                    FROM tbl_Users u
                    INNER JOIN tbl_Roles r ON u.role_id = r.role_id
                    ORDER BY u.created_date DESC";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new UserDto
                    {
                        UserId = (int)reader["user_id"],
                        FirstName = reader["first_name"].ToString() ?? "",
                        LastName = reader["last_name"].ToString() ?? "",
                        Email = reader["email"].ToString() ?? "",
                        Phone = reader["phone"] as string,
                        Department = reader["department"] as string,
                        IsActive = (bool)reader["is_active"],
                        LastLogin = reader["last_login"] as DateTime?,
                        RoleId = (int)reader["role_id"],
                        RoleName = reader["role_name"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex.Message}");
            }

            return users;
        }

        // Get all roles for dropdown
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = new List<RoleDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT role_id, role_name, description FROM tbl_Roles WHERE is_active = 1 ORDER BY role_name";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    roles.Add(new RoleDto
                    {
                        RoleId = (int)reader["role_id"],
                        RoleName = reader["role_name"].ToString() ?? "",
                        Description = reader["description"] as string
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching roles: {ex.Message}");
            }

            return roles;
        }

        // Add new user
        public async Task<ServiceResponse> AddUserAsync(UserCreateDto user, string password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if email exists
                var checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE email = @Email";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", user.Email);
                var count = (int)await checkCommand.ExecuteScalarAsync();

                if (count > 0)
                {
                    return new ServiceResponse { Success = false, Message = "Email already exists" };
                }

                // Generate salt and hash password
                var salt = GenerateSalt();
                var passwordHash = HashPassword(password, salt);

                var query = @"
                    INSERT INTO tbl_Users 
                    (role_id, email, password_hash, salt, first_name, last_name, phone, department, is_active, created_date, modified_date)
                    VALUES 
                    (@RoleId, @Email, @PasswordHash, @Salt, @FirstName, @LastName, @Phone, @Department, @IsActive, GETDATE(), GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RoleId", user.RoleId);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Salt", salt);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Department", (object?)user.Department ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsActive", user.IsActive);

                var userId = (int)await command.ExecuteScalarAsync();

                // Log activity
                await LogActivity(_authService.CurrentUser?.UserId ?? 0, "USER_CREATE", "tbl_Users", userId, "INSERT", connection);

                return new ServiceResponse { Success = true, Message = "User created successfully" };
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Update user
        public async Task<ServiceResponse> UpdateUserAsync(UserUpdateDto user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if email exists for other users
                var checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE email = @Email AND user_id != @UserId";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", user.Email);
                checkCommand.Parameters.AddWithValue("@UserId", user.UserId);
                var count = (int)await checkCommand.ExecuteScalarAsync();

                if (count > 0)
                {
                    return new ServiceResponse { Success = false, Message = "Email already exists" };
                }

                var query = @"
                    UPDATE tbl_Users 
                    SET 
                        role_id = @RoleId,
                        email = @Email,
                        first_name = @FirstName,
                        last_name = @LastName,
                        phone = @Phone,
                        department = @Department,
                        modified_date = GETDATE()
                    WHERE user_id = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", user.UserId);
                command.Parameters.AddWithValue("@RoleId", user.RoleId);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Department", (object?)user.Department ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();

                // Log activity
                await LogActivity(_authService.CurrentUser?.UserId ?? 0, "USER_UPDATE", "tbl_Users", user.UserId, "UPDATE", connection);

                return new ServiceResponse { Success = true, Message = "User updated successfully" };
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Toggle user active status
        public async Task<ServiceResponse> ToggleUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    UPDATE tbl_Users 
                    SET 
                        is_active = @IsActive,
                        modified_date = GETDATE()
                    WHERE user_id = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@IsActive", isActive);

                await command.ExecuteNonQueryAsync();

                var action = isActive ? "ACTIVATE" : "DEACTIVATE";
                await LogActivity(_authService.CurrentUser?.UserId ?? 0, $"USER_{action}", "tbl_Users", userId, "UPDATE", connection);

                var message = isActive ? "User activated successfully" : "User deactivated successfully";
                return new ServiceResponse { Success = true, Message = message };
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Get permissions for a role
        public async Task<List<PermissionDto>> GetRolePermissionsAsync(string roleName)
        {
            var permissions = new List<PermissionDto>();

            var allPermissions = GetPermissionsForRole(roleName);

            return allPermissions;
        }

        // Helper methods
        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private async Task LogActivity(int userId, string activityType, string tableName, int recordId, string action, SqlConnection connection)
        {
            var query = @"
                INSERT INTO tbl_Activity_Log (user_id, activity_type, table_name, record_id, action, created_date)
                VALUES (@UserId, @ActivityType, @TableName, @RecordId, @Action, GETDATE())";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ActivityType", activityType);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@RecordId", recordId);
            command.Parameters.AddWithValue("@Action", action);
            await command.ExecuteNonQueryAsync();
        }

        private List<PermissionDto> GetPermissionsForRole(string role)
        {
            return role switch
            {
                "Admin" => new List<PermissionDto>
                {
                    new() { Name = "View all customers", IsGranted = true },
                    new() { Name = "Edit all customers", IsGranted = true },
                    new() { Name = "Delete customers", IsGranted = true },
                    new() { Name = "View all transactions", IsGranted = true },
                    new() { Name = "Create invoices", IsGranted = true },
                    new() { Name = "Edit invoices", IsGranted = true },
                    new() { Name = "Void invoices", IsGranted = true },
                    new() { Name = "Manage campaigns", IsGranted = true },
                    new() { Name = "View reports", IsGranted = true },
                    new() { Name = "Manage users", IsGranted = true },
                    new() { Name = "System settings", IsGranted = true }
                },
                "Sales" => new List<PermissionDto>
                {
                    new() { Name = "View own customers", IsGranted = true },
                    new() { Name = "Edit own customers", IsGranted = true },
                    new() { Name = "Manage sales pipeline", IsGranted = true },
                    new() { Name = "View basic reports", IsGranted = true },
                    new() { Name = "Delete customers", IsGranted = false },
                    new() { Name = "Access financial data", IsGranted = false }
                },
                "Marketing" => new List<PermissionDto>
                {
                    new() { Name = "View all customers", IsGranted = true },
                    new() { Name = "Create campaigns", IsGranted = true },
                    new() { Name = "View marketing reports", IsGranted = true },
                    new() { Name = "Edit invoices", IsGranted = false },
                    new() { Name = "Manage users", IsGranted = false }
                },
                "Finance" => new List<PermissionDto>
                {
                    new() { Name = "View all customers", IsGranted = true },
                    new() { Name = "Create invoices", IsGranted = true },
                    new() { Name = "Edit invoices", IsGranted = true },
                    new() { Name = "Record payments", IsGranted = true },
                    new() { Name = "View financial reports", IsGranted = true },
                    new() { Name = "Delete customers", IsGranted = false },
                    new() { Name = "Manage users", IsGranted = false }
                },
                _ => new List<PermissionDto>()
            };
        }
    }

    // DTOs
    public class UserDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName => $"{FirstName} {LastName}";
        public string Initials => GetInitials();
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";

        private string GetInitials()
        {
            if (string.IsNullOrEmpty(FirstName)) return "U";
            if (string.IsNullOrEmpty(LastName)) return FirstName[0].ToString().ToUpper();
            return $"{FirstName[0]}{LastName[0]}".ToUpper();
        }
    }

    public class UserCreateDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserUpdateDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public int RoleId { get; set; }
    }

    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
    }

    public class PermissionDto
    {
        public string Name { get; set; } = "";
        public bool IsGranted { get; set; }
    }

    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}