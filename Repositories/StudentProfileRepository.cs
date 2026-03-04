using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;

namespace PATHFINDER_BACKEND.Repositories
{
    public class StudentProfileRepository
    {
        private readonly Db _db;

        public StudentProfileRepository(Db db)
        {
            _db = db;
        }

        // ✅ Auto-create table if it doesn't exist (SQL Server)
        public async Task EnsureTableAsync()
        {
            var sql = @"
IF OBJECT_ID('dbo.student_profiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_profiles (
        student_id INT NOT NULL PRIMARY KEY,
        skills NVARCHAR(MAX) NULL,
        education NVARCHAR(MAX) NULL,
        experience NVARCHAR(MAX) NULL,
        cv_url NVARCHAR(500) NULL,
        updated_at_utc DATETIME2 NULL,

        CONSTRAINT FK_student_profiles_students
            FOREIGN KEY (student_id) REFERENCES dbo.students(id)
            ON DELETE CASCADE
    );
END;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<StudentProfileResponse?> GetStudentProfileAsync(int studentId)
        {
            var sql = @"
SELECT 
    s.id,
    s.full_name,
    s.email,
    p.skills,
    p.education,
    p.experience,
    p.cv_url,
    p.updated_at_utc
FROM dbo.students s
LEFT JOIN dbo.student_profiles p ON p.student_id = s.id
WHERE s.id = @studentId;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new StudentProfileResponse
            {
                StudentId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),

                Skills = reader.IsDBNull(3) ? null : reader.GetString(3),
                Education = reader.IsDBNull(4) ? null : reader.GetString(4),
                Experience = reader.IsDBNull(5) ? null : reader.GetString(5),

                CvUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                UpdatedAtUtc = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        // ✅ Upsert profile row (insert or update)
        public async Task UpsertStudentProfileAsync(
            int studentId,
            string? skills,
            string? education,
            string? experience,
            string? cvUrl
        )
        {
            var sql = @"
IF EXISTS (SELECT 1 FROM dbo.student_profiles WHERE student_id = @studentId)
BEGIN
    UPDATE dbo.student_profiles
    SET 
        skills = @skills,
        education = @education,
        experience = @experience,
        cv_url = COALESCE(@cvUrl, cv_url),
        updated_at_utc = SYSUTCDATETIME()
    WHERE student_id = @studentId;
END
ELSE
BEGIN
    INSERT INTO dbo.student_profiles (student_id, skills, education, experience, cv_url, updated_at_utc)
    VALUES (@studentId, @skills, @education, @experience, @cvUrl, SYSUTCDATETIME());
END
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);

            cmd.Parameters.AddWithValue("@skills", (object?)skills ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@education", (object?)education ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@experience", (object?)experience ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cvUrl", (object?)cvUrl ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}