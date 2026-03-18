using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;

namespace PATHFINDER_BACKEND.Repositories
{
    public class ApplicationRepository
    {
        private readonly Db _db;

        public ApplicationRepository(Db db)
        {
            _db = db;
        }

        /// <summary>
        /// Creates the applications table (if not exists) with a unique constraint on (student_id, job_id).
        /// Also adds any missing columns safely (idempotent migration pattern).
        /// </summary>
        public async Task EnsureTableAndConstraintsAsync()
        {
            var sql = @"
IF OBJECT_ID('dbo.applications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.applications (
        id INT IDENTITY(1,1) PRIMARY KEY,
        student_id INT NOT NULL,
        job_id INT NOT NULL,
        status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        applied_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        cover_letter NVARCHAR(MAX) NULL,

        CONSTRAINT FK_applications_students
            FOREIGN KEY (student_id) REFERENCES dbo.students(id)
            ON DELETE CASCADE,

        CONSTRAINT FK_applications_jobs
            FOREIGN KEY (job_id) REFERENCES dbo.jobs(id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_applications_student_job
            UNIQUE (student_id, job_id)
    );
END;

-- Add missing columns safely if table already exists
IF COL_LENGTH('dbo.applications', 'student_id') IS NULL ALTER TABLE dbo.applications ADD student_id INT NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.applications', 'job_id') IS NULL ALTER TABLE dbo.applications ADD job_id INT NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.applications', 'status') IS NULL ALTER TABLE dbo.applications ADD status NVARCHAR(50) NOT NULL DEFAULT 'Pending';
IF COL_LENGTH('dbo.applications', 'applied_date') IS NULL ALTER TABLE dbo.applications ADD applied_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('dbo.applications', 'cover_letter') IS NULL ALTER TABLE dbo.applications ADD cover_letter NVARCHAR(MAX) NULL;

-- Ensure the unique constraint exists
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE name = 'UQ_applications_student_job' AND type = 'UQ'
)
BEGIN
    ALTER TABLE dbo.applications
        ADD CONSTRAINT UQ_applications_student_job UNIQUE (student_id, job_id);
END;

-- Index on student_id for fast lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_applications_student_id' AND object_id = OBJECT_ID('dbo.applications'))
    CREATE INDEX idx_applications_student_id ON dbo.applications(student_id);

-- Index on job_id for fast lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_applications_job_id' AND object_id = OBJECT_ID('dbo.applications'))
    CREATE INDEX idx_applications_job_id ON dbo.applications(job_id);
";
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Checks whether a student has already applied for a given job.
        /// </summary>
        public async Task<bool> HasStudentAppliedAsync(int studentId, int jobId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.applications
WHERE student_id = @studentId AND job_id = @jobId;
";
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        /// <summary>
        /// Inserts a new application record with status = 'Pending'.
        /// Returns the newly created application id.
        /// </summary>
        public async Task<int> ApplyForJobAsync(int studentId, int jobId, string? coverLetter)
        {
            const string sql = @"
INSERT INTO dbo.applications (student_id, job_id, status, applied_date, cover_letter)
VALUES (@studentId, @jobId, 'Pending', SYSUTCDATETIME(), @coverLetter);
SELECT CAST(SCOPE_IDENTITY() AS INT);
";
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            cmd.Parameters.AddWithValue("@coverLetter", (object?)coverLetter ?? DBNull.Value);
            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return newId;
        }

        /// <summary>
        /// Gets the total number of applications submitted by a student.
        /// </summary>
        public async Task<int> GetStudentApplicationCountAsync(int studentId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.applications
WHERE student_id = @studentId;
";
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
    }
}
