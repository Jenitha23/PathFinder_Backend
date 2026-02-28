using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Repository layer for Admin operations.
    /// Encapsulates all SQL queries and keeps controllers clean.
    /// </summary>
    public class AdminRepository
    {
        private readonly Db _db;
        public AdminRepository(Db db) => _db = db;

        /// <summary>
        /// Fetch an admin user by email.
        /// Returns null if admin does not exist.
        /// </summary>
        public async Task<Admin?> GetByEmailAsync(string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Parameterized query prevents SQL injection.
            const string sql = @"
                SELECT TOP 1 id, full_name, email, password_hash, created_at
                FROM admins
                WHERE email = @email;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Admin
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            };
        }

        /// <summary>
        /// Fetch an admin user by ID.
        /// Useful for "me" profile endpoint.
        /// </summary>
        public async Task<Admin?> GetByIdAsync(int id)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP 1 id, full_name, email, password_hash, created_at
                FROM admins
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Admin
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            };
        }

        /// <summary>
        /// Create a new admin and return the generated identity ID.
        /// Typically used only for seeding or internal admin creation.
        /// </summary>
        public async Task<int> CreateAsync(Admin a)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO admins (full_name, email, password_hash)
                VALUES (@full, @email, @hash);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", a.FullName);
            cmd.Parameters.AddWithValue("@email", a.Email);
            cmd.Parameters.AddWithValue("@hash", a.PasswordHash);

            var idObj = await cmd.ExecuteScalarAsync();
            return (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);
        }

        /// <summary>
        /// Update admin profile fields (full name + email).
        /// Ensures controller handles email uniqueness rules.
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int adminId, string fullName, string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE admins
                SET full_name = @full, email = @email
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", fullName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@id", adminId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Update stored password hash for an admin.
        /// Called after verifying current password and hashing new password.
        /// </summary>
        public async Task<bool> UpdatePasswordHashAsync(int adminId, string passwordHash)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE admins
                SET password_hash = @hash
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", passwordHash);
            cmd.Parameters.AddWithValue("@id", adminId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Ensures a default admin exists (seed admin).
        /// Used at application startup.
        /// </summary>
        public async Task EnsureSeedAdminAsync(string fullName, string email, string passwordHash)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Idempotent seed: inserts only if admin email doesn't exist.
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM admins WHERE email = @email)
                BEGIN
                    INSERT INTO admins (full_name, email, password_hash)
                    VALUES (@full, @email, @hash);
                END";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", fullName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@hash", passwordHash);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}