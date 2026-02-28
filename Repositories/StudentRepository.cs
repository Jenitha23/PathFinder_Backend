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
    }
}