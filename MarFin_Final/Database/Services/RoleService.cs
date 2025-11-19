using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MarFin_Final.Models;

namespace MarFin_Final.Services
{
    public class RoleService
    {
        private readonly string _connectionString;

        public RoleService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration");
        }

        // Get all active roles
        public async Task<List<Role>> GetActiveRolesAsync()
        {
            var roles = new List<Role>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT role_id, role_name, description, permissions, is_active, created_date, modified_date
                    FROM tbl_Roles
                    WHERE is_active = 1
                    ORDER BY role_name";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    roles.Add(new Role
                    {
                        RoleId = (int)reader["role_id"],
                        RoleName = reader["role_name"].ToString() ?? "",
                        Description = reader["description"] as string,
                        Permissions = reader["permissions"] as string,
                        IsActive = (bool)reader["is_active"],
                        CreatedDate = reader["created_date"] as DateTime?,
                        ModifiedDate = reader["modified_date"] as DateTime?
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error or handle it appropriately
                throw new Exception($"Error fetching roles: {ex.Message}", ex);
            }

            return roles;
        }

        // Get role by ID
        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT role_id, role_name, description, permissions, is_active, created_date, modified_date
                    FROM tbl_Roles
                    WHERE role_id = @RoleId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RoleId", roleId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new Role
                    {
                        RoleId = (int)reader["role_id"],
                        RoleName = reader["role_name"].ToString() ?? "",
                        Description = reader["description"] as string,
                        Permissions = reader["permissions"] as string,
                        IsActive = (bool)reader["is_active"],
                        CreatedDate = reader["created_date"] as DateTime?,
                        ModifiedDate = reader["modified_date"] as DateTime?
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching role: {ex.Message}", ex);
            }

            return null;
        }
    }
}