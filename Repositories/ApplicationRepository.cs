using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;

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

        /// <summary>
        /// Gets all applications for a student, joined with job and company details.
        /// Supports optional filtering by status and sorting by applied date.
        /// </summary>
        public async Task<List<ApplicationResponse>> GetStudentApplicationsAsync(
            int studentId,
            string? status = null,
            string sortBy = "date_desc")
        {
            var whereClauses = new List<string> { "a.student_id = @studentId" };
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@studentId", studentId)
            };

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                whereClauses.Add("a.status = @status");
                parameters.Add(new SqlParameter("@status", status.Trim()));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            // Determine sort order
            var orderSql = sortBy?.ToLower() == "date_asc"
                ? "ORDER BY a.applied_date ASC"
                : "ORDER BY a.applied_date DESC";

            var sql = $@"
SELECT
    a.id,
    j.id,
    j.title,
    c.company_name,
    j.location,
    j.type,
    a.status,
    a.applied_date
FROM dbo.applications a
INNER JOIN dbo.jobs j ON a.job_id = j.id
INNER JOIN dbo.companies c ON j.company_id = c.id
{whereSql}
{orderSql};
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            var results = new List<ApplicationResponse>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ApplicationResponse
                {
                    ApplicationId = reader.GetInt32(0),
                    JobId = reader.GetInt32(1),
                    JobTitle = reader.GetString(2),
                    CompanyName = reader.GetString(3),
                    Location = reader.GetString(4),
                    JobType = reader.GetString(5),
                    Status = reader.GetString(6),
                    AppliedDate = reader.GetDateTime(7)
                });
            }

            return results;
        }

        // ============= NEW METHODS FOR COMPANY APPLICANT MANAGEMENT =============

        /// <summary>
        /// Gets all applicants for a specific job with company ownership validation.
        /// Includes student profile data from student_profiles table.
        /// </summary>
        public async Task<List<ApplicantListResponse>> GetApplicantsByJobIdAsync(int companyId, int jobId, string? statusFilter = null)
        {
            var whereClauses = new List<string> { 
                "j.company_id = @companyId", 
                "j.id = @jobId",
                "(j.is_deleted IS NULL OR j.is_deleted = 0)"
            };
            
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@companyId", companyId),
                new SqlParameter("@jobId", jobId)
            };

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
                if (validStatuses.Contains(statusFilter))
                {
                    whereClauses.Add("a.status = @status");
                    parameters.Add(new SqlParameter("@status", statusFilter));
                }
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);
            
            var sql = $@"
SELECT
    a.id AS ApplicationId,
    s.id AS StudentId,
    s.full_name AS StudentName,
    s.email AS StudentEmail,
    sp.headline,
    sp.skills,
    sp.technical_skills AS TechnicalSkills,
    sp.education,
    sp.university,
    sp.degree,
    sp.cv_url AS CvUrl,
    a.status,
    a.cover_letter AS CoverLetter,
    a.applied_date AS AppliedDate
FROM dbo.applications a
INNER JOIN dbo.jobs j ON a.job_id = j.id
INNER JOIN dbo.students s ON a.student_id = s.id
LEFT JOIN dbo.student_profiles sp ON sp.student_id = s.id
{whereSql}
ORDER BY a.applied_date DESC;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parameters)
                cmd.Parameters.Add(p);
            
            var results = new List<ApplicantListResponse>();
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new ApplicantListResponse
                {
                    ApplicationId = reader.GetInt32(reader.GetOrdinal("ApplicationId")),
                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                    StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                    StudentEmail = reader.GetString(reader.GetOrdinal("StudentEmail")),
                    Headline = reader.IsDBNull(reader.GetOrdinal("headline")) ? null : reader.GetString(reader.GetOrdinal("headline")),
                    Skills = reader.IsDBNull(reader.GetOrdinal("skills")) ? null : reader.GetString(reader.GetOrdinal("skills")),
                    TechnicalSkills = reader.IsDBNull(reader.GetOrdinal("TechnicalSkills")) ? null : reader.GetString(reader.GetOrdinal("TechnicalSkills")),
                    Education = reader.IsDBNull(reader.GetOrdinal("education")) ? null : reader.GetString(reader.GetOrdinal("education")),
                    University = reader.IsDBNull(reader.GetOrdinal("university")) ? null : reader.GetString(reader.GetOrdinal("university")),
                    Degree = reader.IsDBNull(reader.GetOrdinal("degree")) ? null : reader.GetString(reader.GetOrdinal("degree")),
                    CvUrl = reader.IsDBNull(reader.GetOrdinal("CvUrl")) ? null : reader.GetString(reader.GetOrdinal("CvUrl")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CoverLetter = reader.IsDBNull(reader.GetOrdinal("CoverLetter")) ? null : reader.GetString(reader.GetOrdinal("CoverLetter")),
                    AppliedDate = reader.GetDateTime(reader.GetOrdinal("AppliedDate"))
                });
            }
            
            return results;
        }

        /// <summary>
        /// Gets detailed applicant information including full student profile.
        /// Validates that the company owns the job before returning data.
        /// </summary>
        public async Task<ApplicantDetailsResponse?> GetApplicantDetailsAsync(int companyId, int jobId, int applicationId)
        {
            var sql = @"
SELECT
    a.id AS ApplicationId,
    s.id AS StudentId,
    s.full_name AS StudentName,
    s.email AS StudentEmail,
    
    -- Profile Information
    sp.headline,
    sp.about_me AS AboutMe,
    sp.skills,
    sp.technical_skills AS TechnicalSkills,
    sp.soft_skills AS SoftSkills,
    sp.languages,
    sp.education,
    sp.experience,
    
    -- Contact Information
    sp.phone,
    sp.address,
    sp.city,
    sp.country,
    
    -- Education Details
    sp.university,
    sp.degree,
    sp.academic_year AS AcademicYear,
    sp.gpa,
    
    -- Career Preferences
    sp.career_interests AS CareerInterests,
    sp.preferred_job_type AS PreferredJobType,
    sp.work_mode AS WorkMode,
    sp.available_from AS AvailableFrom,
    
    -- Links
    sp.github_url AS GithubUrl,
    sp.linkedin_url AS LinkedinUrl,
    sp.portfolio_url AS PortfolioUrl,
    
    -- Projects & Certifications
    sp.projects_summary AS ProjectsSummary,
    sp.internship_experience AS InternshipExperience,
    sp.certifications,
    
    -- Application Details
    a.cover_letter AS CoverLetter,
    a.status,
    a.applied_date AS AppliedDate,
    
    -- CV
    sp.cv_url AS CvUrl
    
FROM dbo.applications a
INNER JOIN dbo.jobs j ON a.job_id = j.id
INNER JOIN dbo.students s ON a.student_id = s.id
LEFT JOIN dbo.student_profiles sp ON sp.student_id = s.id
WHERE j.company_id = @companyId 
    AND j.id = @jobId 
    AND a.id = @applicationId
    AND (j.is_deleted IS NULL OR j.is_deleted = 0);
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            cmd.Parameters.AddWithValue("@applicationId", applicationId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            
            return new ApplicantDetailsResponse
            {
                ApplicationId = reader.GetInt32(reader.GetOrdinal("ApplicationId")),
                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                StudentEmail = reader.GetString(reader.GetOrdinal("StudentEmail")),
                
                Headline = reader.IsDBNull(reader.GetOrdinal("headline")) ? null : reader.GetString(reader.GetOrdinal("headline")),
                AboutMe = reader.IsDBNull(reader.GetOrdinal("AboutMe")) ? null : reader.GetString(reader.GetOrdinal("AboutMe")),
                Skills = reader.IsDBNull(reader.GetOrdinal("skills")) ? null : reader.GetString(reader.GetOrdinal("skills")),
                TechnicalSkills = reader.IsDBNull(reader.GetOrdinal("TechnicalSkills")) ? null : reader.GetString(reader.GetOrdinal("TechnicalSkills")),
                SoftSkills = reader.IsDBNull(reader.GetOrdinal("SoftSkills")) ? null : reader.GetString(reader.GetOrdinal("SoftSkills")),
                Languages = reader.IsDBNull(reader.GetOrdinal("languages")) ? null : reader.GetString(reader.GetOrdinal("languages")),
                Education = reader.IsDBNull(reader.GetOrdinal("education")) ? null : reader.GetString(reader.GetOrdinal("education")),
                Experience = reader.IsDBNull(reader.GetOrdinal("experience")) ? null : reader.GetString(reader.GetOrdinal("experience")),
                
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                Country = reader.IsDBNull(reader.GetOrdinal("country")) ? null : reader.GetString(reader.GetOrdinal("country")),
                
                University = reader.IsDBNull(reader.GetOrdinal("university")) ? null : reader.GetString(reader.GetOrdinal("university")),
                Degree = reader.IsDBNull(reader.GetOrdinal("degree")) ? null : reader.GetString(reader.GetOrdinal("degree")),
                AcademicYear = reader.IsDBNull(reader.GetOrdinal("AcademicYear")) ? null : reader.GetString(reader.GetOrdinal("AcademicYear")),
                Gpa = reader.IsDBNull(reader.GetOrdinal("gpa")) ? null : reader.GetString(reader.GetOrdinal("gpa")),
                
                CareerInterests = reader.IsDBNull(reader.GetOrdinal("CareerInterests")) ? null : reader.GetString(reader.GetOrdinal("CareerInterests")),
                PreferredJobType = reader.IsDBNull(reader.GetOrdinal("PreferredJobType")) ? null : reader.GetString(reader.GetOrdinal("PreferredJobType")),
                WorkMode = reader.IsDBNull(reader.GetOrdinal("WorkMode")) ? null : reader.GetString(reader.GetOrdinal("WorkMode")),
                AvailableFrom = reader.IsDBNull(reader.GetOrdinal("AvailableFrom")) ? null : reader.GetDateTime(reader.GetOrdinal("AvailableFrom")),
                
                GithubUrl = reader.IsDBNull(reader.GetOrdinal("GithubUrl")) ? null : reader.GetString(reader.GetOrdinal("GithubUrl")),
                LinkedinUrl = reader.IsDBNull(reader.GetOrdinal("LinkedinUrl")) ? null : reader.GetString(reader.GetOrdinal("LinkedinUrl")),
                PortfolioUrl = reader.IsDBNull(reader.GetOrdinal("PortfolioUrl")) ? null : reader.GetString(reader.GetOrdinal("PortfolioUrl")),
                
                ProjectsSummary = reader.IsDBNull(reader.GetOrdinal("ProjectsSummary")) ? null : reader.GetString(reader.GetOrdinal("ProjectsSummary")),
                InternshipExperience = reader.IsDBNull(reader.GetOrdinal("InternshipExperience")) ? null : reader.GetString(reader.GetOrdinal("InternshipExperience")),
                Certifications = reader.IsDBNull(reader.GetOrdinal("certifications")) ? null : reader.GetString(reader.GetOrdinal("certifications")),
                
                CoverLetter = reader.IsDBNull(reader.GetOrdinal("CoverLetter")) ? null : reader.GetString(reader.GetOrdinal("CoverLetter")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                AppliedDate = reader.GetDateTime(reader.GetOrdinal("AppliedDate")),
                
                CvUrl = reader.IsDBNull(reader.GetOrdinal("CvUrl")) ? null : reader.GetString(reader.GetOrdinal("CvUrl"))
            };
        }

        /// <summary>
        /// Updates the status of an application with company ownership validation.
        /// Returns true if update was successful, false if validation fails.
        /// </summary>
        public async Task<bool> UpdateApplicationStatusAsync(int companyId, int jobId, int applicationId, string newStatus)
        {
            // First validate that the company owns the job and the application belongs to the job
            var validationSql = @"
SELECT COUNT(1)
FROM dbo.applications a
INNER JOIN dbo.jobs j ON a.job_id = j.id
WHERE j.company_id = @companyId 
    AND j.id = @jobId 
    AND a.id = @applicationId
    AND (j.is_deleted IS NULL OR j.is_deleted = 0);
";
            
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            // Validate ownership
            using (var validateCmd = new SqlCommand(validationSql, conn))
            {
                validateCmd.Parameters.AddWithValue("@companyId", companyId);
                validateCmd.Parameters.AddWithValue("@jobId", jobId);
                validateCmd.Parameters.AddWithValue("@applicationId", applicationId);
                
                var count = Convert.ToInt32(await validateCmd.ExecuteScalarAsync());
                if (count == 0)
                {
                    return false; // Company doesn't own this job or application doesn't exist
                }
            }
            
            // Update the status
            var updateSql = @"
UPDATE dbo.applications
SET status = @status
WHERE id = @applicationId;
";
            
            using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@status", newStatus);
            updateCmd.Parameters.AddWithValue("@applicationId", applicationId);
            
            var rowsAffected = await updateCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Verifies that a company owns a specific job.
        /// Used for authorization before allowing status updates.
        /// </summary>
        public async Task<bool> VerifyCompanyOwnsJobAsync(int companyId, int jobId)
        {
            var sql = @"
SELECT COUNT(1)
FROM dbo.jobs
WHERE id = @jobId 
    AND company_id = @companyId
    AND (is_deleted IS NULL OR is_deleted = 0);
";
            
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }
    }
}