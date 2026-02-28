using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Handles all database operations related to Company entities.
    /// Keeps SQL logic separated from controllers (clean architecture principle).
    /// </summary>
    public class CompanyRepository
    {
        private readonly Db _db;
        public CompanyRepository(Db db) => _db = db;

        /// <summary>
        /// Fetch a company by ID.
        /// Returns null if no matching record exists.
        /// </summary>
        public async Task<Company?> GetByIdAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1) id, company_name, email, password_hash, status, created_at
                FROM companies
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Company
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        /// <summary>
        /// Fetch a company by email.
        /// Returns null if no matching record exists.
        /// </summary>
        public async Task<Company?> GetByEmailAsync(string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Parameterized query prevents SQL injection.
            const string sql = @"
                SELECT TOP (1) id, company_name, email, password_hash, status, created_at
                FROM companies
                WHERE email = @email;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            // Using column ordinals improves performance and avoids column-order dependency.
            var idIdx = reader.GetOrdinal("id");
            var nameIdx = reader.GetOrdinal("company_name");
            var emailIdx = reader.GetOrdinal("email");
            var hashIdx = reader.GetOrdinal("password_hash");
            var statusIdx = reader.GetOrdinal("status");
            var createdIdx = reader.GetOrdinal("created_at");

            return new Company
            {
                Id = reader.GetInt32(idIdx),
                CompanyName = reader.GetString(nameIdx),
                Email = reader.GetString(emailIdx),
                PasswordHash = reader.GetString(hashIdx),
                Status = reader.GetString(statusIdx),
                CreatedAt = reader.GetDateTime(createdIdx)
            };
        }

        /// <summary>
        /// Returns all companies (excluding password hash).
        /// Useful for admin approval dashboard.
        /// </summary>
        public async Task<List<Company>> GetAllAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, company_name, email, status, created_at
                FROM companies
                ORDER BY id DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var companies = new List<Company>();
            while (await reader.ReadAsync())
            {
                companies.Add(new Company
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return companies;
        }

        /// <summary>
        /// Creates a new company record and returns generated ID.
        /// Status is typically PENDING_APPROVAL.
        /// </summary>
        public async Task<int> CreateAsync(Company c)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO companies (company_name, email, password_hash, status)
                VALUES (@name, @email, @hash, @status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", c.CompanyName);
            cmd.Parameters.AddWithValue("@email", c.Email);
            cmd.Parameters.AddWithValue("@hash", c.PasswordHash);
            cmd.Parameters.AddWithValue("@status", c.Status);

            var idObj = await cmd.ExecuteScalarAsync();
            return (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);
        }

        /// <summary>
        /// Updates company approval status (used by Admin).
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int companyId, string status)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET status = @status
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates company profile fields (name + email).
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int companyId, string companyName, string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET company_name = @name, email = @email
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", companyName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates company password hash.
        /// </summary>
        public async Task<bool> UpdatePasswordHashAsync(int companyId, string passwordHash)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET password_hash = @hash
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", passwordHash);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Deletes a company by ID.
        /// </summary>
        public async Task<bool> DeleteByIdAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                DELETE FROM companies
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
