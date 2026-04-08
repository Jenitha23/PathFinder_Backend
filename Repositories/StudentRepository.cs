using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Repository = database access layer.
    /// Keeps SQL isolated from controllers/services (clean architecture).
    /// </summary>
    public class StudentRepository
    {
        private readonly Db _db;
        public StudentRepository(Db db) => _db = db;

        /// <summary>
        /// Fetch a student by ID.
        /// Returns null if no user exists.
        /// </summary>
        public async Task<Student?> GetByIdAsync(int id)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1) id, full_name, email, password_hash, created_at
                FROM students
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Student
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        /// <summary>
        /// Fetch a student by email.
        /// Returns null if no user exists.
        /// </summary>
        public async Task<Student?> GetByEmailAsync(string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Parameterized query prevents SQL injection.
            const string sql = @"
                SELECT TOP (1) id, full_name, email, password_hash, created_at
                FROM students
                WHERE email = @email;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            // Ordinals are used for safer access by column name.
            var idIdx = reader.GetOrdinal("id");
            var fullIdx = reader.GetOrdinal("full_name");
            var emailIdx = reader.GetOrdinal("email");
            var hashIdx = reader.GetOrdinal("password_hash");
            var createdIdx = reader.GetOrdinal("created_at");

            return new Student
            {
                Id = reader.GetInt32(idIdx),
                FullName = reader.GetString(fullIdx),
                Email = reader.GetString(emailIdx),
                PasswordHash = reader.GetString(hashIdx),
                CreatedAt = reader.GetDateTime(createdIdx)
            };
        }

        /// <summary>
        /// Returns all students (without password hash).
        /// Useful for admin views or debugging (restrict this endpoint in controllers if exposed).
        /// </summary>
        public async Task<List<Student>> GetAllAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Password hash is intentionally not selected for safety.
            const string sql = @"
                SELECT id, full_name, email, created_at
                FROM students
                ORDER BY id DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var students = new List<Student>();
            while (await reader.ReadAsync())
            {
                students.Add(new Student
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    FullName = reader.GetString(reader.GetOrdinal("full_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return students;
        }

        /// <summary>
        /// Creates a new student record and returns the generated ID.
        /// </summary>
        public async Task<int> CreateAsync(Student s)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO students (full_name, email, password_hash)
                VALUES (@full, @email, @hash);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", s.FullName);
            cmd.Parameters.AddWithValue("@email", s.Email);
            cmd.Parameters.AddWithValue("@hash", s.PasswordHash);

            var idObj = await cmd.ExecuteScalarAsync();
            return (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);
        }

        /// <summary>
        /// Updates student profile fields (full name + email).
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int studentId, string fullName, string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE students
                SET full_name = @full, email = @email
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", fullName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@id", studentId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates student password hash.
        /// </summary>
        public async Task<bool> UpdatePasswordHashAsync(int studentId, string passwordHash)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE students
                SET password_hash = @hash
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", passwordHash);
            cmd.Parameters.AddWithValue("@id", studentId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Deletes a student by ID.
        /// </summary>
        public async Task<bool> DeleteByIdAsync(int studentId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                DELETE FROM students
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", studentId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        // ========== NEW METHODS FOR ADMIN USER MANAGEMENT ==========

        /// <summary>
        /// Get all students with optional filtering and pagination (for admin user management)
        /// </summary>
        public async Task<(List<Student> Students, int TotalCount)> GetAllWithFilterAsync(
            string? searchTerm = null,
            string? status = null,
            int page = 1,
            int pageSize = 20)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT id, full_name, email, password_hash, created_at, 
                       status, is_deleted, deleted_at, updated_at, suspension_reason
                FROM students
                WHERE is_deleted = 0";

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            // Search by name or email
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add("(full_name LIKE @search OR email LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{searchTerm.Trim()}%"));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
            {
                conditions.Add("status = @status");
                parameters.Add(new SqlParameter("@status", status));
            }

            if (conditions.Any())
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            // Get total count
            var countSql = sql.Replace(
                "SELECT id, full_name, email, password_hash, created_at, status, is_deleted, deleted_at, updated_at, suspension_reason",
                "SELECT COUNT(*)");

            await using var countCmd = new SqlCommand(countSql, conn);
            foreach (var param in parameters)
            {
                countCmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
            }
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Add pagination
            sql += " ORDER BY created_at DESC OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            parameters.Add(new SqlParameter("@offset", (page - 1) * pageSize));
            parameters.Add(new SqlParameter("@pageSize", pageSize));

            await using var cmd = new SqlCommand(sql, conn);
            foreach (var param in parameters)
            {
                cmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
            }

            var students = new List<Student>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                students.Add(new Student
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    FullName = reader.GetString(reader.GetOrdinal("full_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "ACTIVE" : reader.GetString(reader.GetOrdinal("status")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                    DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    SuspensionReason = reader.IsDBNull(reader.GetOrdinal("suspension_reason")) ? null : reader.GetString(reader.GetOrdinal("suspension_reason"))
                });
            }

            return (students, totalCount);
        }

        /// <summary>
        /// Update student account by admin (full name, email, status)
        /// </summary>
        public async Task<bool> UpdateByAdminAsync(int studentId, string fullName, string email, string status, string? suspensionReason = null)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE students
                SET full_name = @fullName, 
                    email = @email,
                    status = @status,
                    suspension_reason = @suspensionReason,
                    updated_at = SYSUTCDATETIME()
                WHERE id = @id AND is_deleted = 0";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@fullName", fullName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@suspensionReason", suspensionReason ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", studentId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Soft delete a student account (marks as deleted but keeps in database)
        /// </summary>
        public async Task<bool> SoftDeleteAsync(int studentId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE students
                SET is_deleted = 1, 
                    deleted_at = SYSUTCDATETIME(),
                    status = 'DELETED',
                    updated_at = SYSUTCDATETIME()
                WHERE id = @id AND is_deleted = 0";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", studentId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Get student by ID including all status fields
        /// </summary>
        public async Task<Student?> GetByIdWithDetailsAsync(int id)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, full_name, email, password_hash, created_at, 
                       status, is_deleted, deleted_at, updated_at, suspension_reason
                FROM students
                WHERE id = @id";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Student
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "ACTIVE" : reader.GetString(reader.GetOrdinal("status")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                SuspensionReason = reader.IsDBNull(reader.GetOrdinal("suspension_reason")) ? null : reader.GetString(reader.GetOrdinal("suspension_reason"))
            };
        }
    }
}

    


