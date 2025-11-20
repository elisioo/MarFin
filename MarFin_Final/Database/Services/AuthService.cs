using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MarFin_Final.Models;
using RoleModel = MarFin_Final.Models.Role;

namespace MarFin_Final.Database.Services
{

    public class AuthService
    {

        private readonly string _connectionString;
        private User? _currentUser;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration");
        }

        // Event to notify components when auth state changes
        public event Action? OnAuthStateChanged;

        public bool IsAuthenticated => _currentUser != null;
        public User? CurrentUser => _currentUser;
        public string CurrentUserName => _currentUser?.FullName ?? "User";
        public string CurrentUserFirstName => _currentUser?.FirstName ?? "User";
        public string CurrentUserRole => _currentUser?.Role?.RoleName ?? "User";

        // Login method
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get user by email
                var query = @"
                    SELECT u.*, r.role_name, r.permissions 
                    FROM tbl_Users u
                    INNER JOIN tbl_Roles r ON u.role_id = r.role_id
                    WHERE u.email = @Email AND u.is_active = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", request.Email);

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var user = MapUserFromReader(reader);
                var storedHash = reader["password_hash"].ToString() ?? "";
                var salt = reader["salt"].ToString() ?? "";

                // Check if account is locked
                if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = $"Account is locked until {user.LockedUntil.Value:g}"
                    };
                }

                // Verify password
                var passwordHash = HashPassword(request.Password, salt);
                if (passwordHash != storedHash)
                {
                    await IncrementFailedLoginAttempts(user.UserId, connection);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Reset failed attempts and update last login
                await UpdateSuccessfulLogin(user.UserId, connection);

                _currentUser = user;
                OnAuthStateChanged?.Invoke(); // Notify all subscribed components

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = user
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Login error: {ex.Message}"
                };
            }
        }

        // Register method
        public async Task<AuthResponse> RegisterAsync(RegisterRequests request)
        {
            try
            {
                // Validate passwords match
                if (request.Password != request.ConfirmPassword)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Passwords do not match"
                    };
                }

                // Validate password strength
                if (!IsPasswordStrong(request.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Password must be at least 8 characters and contain uppercase, lowercase, number, and special character"
                    };
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if email already exists
                var checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE email = @Email";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", request.Email);
                var count = (int)await checkCommand.ExecuteScalarAsync();

                if (count > 0)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email already registered"
                    };
                }

                // Generate salt and hash password
                var salt = GenerateSalt();
                var passwordHash = HashPassword(request.Password, salt);

                // Insert new user
                var insertQuery = @"
                    INSERT INTO tbl_Users 
                    (role_id, email, password_hash, salt, first_name, last_name, phone, department, is_active, created_date, modified_date)
                    VALUES 
                    (@RoleId, @Email, @PasswordHash, @Salt, @FirstName, @LastName, @Phone, @Department, 1, GETDATE(), GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@RoleId", request.RoleId);
                insertCommand.Parameters.AddWithValue("@Email", request.Email);
                insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                insertCommand.Parameters.AddWithValue("@Salt", salt);
                insertCommand.Parameters.AddWithValue("@FirstName", request.FirstName);
                insertCommand.Parameters.AddWithValue("@LastName", request.LastName);
                insertCommand.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Department", (object?)request.Department ?? DBNull.Value);

                var userId = (int)await insertCommand.ExecuteScalarAsync();

                // Log the registration
                await LogActivity(userId, "USER_REGISTRATION", "tbl_Users", userId, "INSERT", connection);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful! Please login."
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration error: {ex.Message}"
                };
            }
        }

        // Logout method
        public void Logout()
        {
            _currentUser = null;
            OnAuthStateChanged?.Invoke(); // Notify all subscribed components
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

        private bool IsPasswordStrong(string password)
        {
            if (password.Length < 8) return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        private async Task IncrementFailedLoginAttempts(int userId, SqlConnection connection)
        {
            var query = @"
                UPDATE tbl_Users 
                SET failed_login_attempts = failed_login_attempts + 1,
                    locked_until = CASE 
                        WHEN failed_login_attempts >= 4 THEN DATEADD(MINUTE, 15, GETDATE())
                        ELSE locked_until 
                    END
                WHERE user_id = @UserId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task UpdateSuccessfulLogin(int userId, SqlConnection connection)
        {
            var query = @"
                UPDATE tbl_Users 
                SET failed_login_attempts = 0,
                    locked_until = NULL,
                    last_login = GETDATE()
                WHERE user_id = @UserId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            await command.ExecuteNonQueryAsync();

            await LogActivity(userId, "USER_LOGIN", "tbl_Users", userId, "LOGIN", connection);
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


        private User MapUserFromReader(SqlDataReader reader)
        {
            return new User
            {
                UserId = (int)reader["user_id"],
                RoleId = (int)reader["role_id"],
                Email = reader["email"].ToString() ?? "",
                FirstName = reader["first_name"].ToString() ?? "",
                LastName = reader["last_name"].ToString() ?? "",
                Phone = reader["phone"] as string,
                Department = reader["department"] as string,
                ProfileImagePath = reader["profile_image_path"] as string,
                IsActive = (bool)reader["is_active"],
                LastLogin = reader["last_login"] as DateTime?,
                FailedLoginAttempts = (int)reader["failed_login_attempts"],
                LockedUntil = reader["locked_until"] as DateTime?,
                CreatedDate = (DateTime)reader["created_date"],
                ModifiedDate = (DateTime)reader["modified_date"],
                Role = new RoleModel
                {
                    RoleId = (int)reader["role_id"],
                    RoleName = reader["role_name"].ToString() ?? "",
                    Permissions = reader["permissions"] as string
                }
            };
        }
    }
}