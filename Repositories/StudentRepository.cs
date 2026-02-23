using MySqlConnector;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    public class StudentRepository
    {
        private readonly Db _db;
        public StudentRepository(Db db) => _db = db;

        public async Task<Student?> GetByEmailAsync(string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, full_name, email, password_hash, created_at
                                 FROM students
                                 WHERE email = @email
                                 LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new Student
            {
                Id = reader.GetInt32("id"),
                FullName = reader.GetString("full_name"),
                Email = reader.GetString("email"),
                PasswordHash = reader.GetString("password_hash"),
                CreatedAt = reader.GetDateTime("created_at")
            };
        }

        public async Task<int> CreateAsync(Student s)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO students(full_name, email, password_hash)
                                 VALUES (@full, @email, @hash);
                                 SELECT LAST_INSERT_ID();";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@full", s.FullName);
            cmd.Parameters.AddWithValue("@email", s.Email);
            cmd.Parameters.AddWithValue("@hash", s.PasswordHash);

            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }
    }
}