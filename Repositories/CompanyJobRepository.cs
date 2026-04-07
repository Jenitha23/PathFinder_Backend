using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;

namespace PATHFINDER_BACKEND.Repositories
{
    public class CompanyJobRepository
    {
        private readonly Db _db;

        public CompanyJobRepository(Db db)
        {
            _db = db;
        }

        public async Task<int> CreateJobAsync(int companyId, CreateJobRequest request)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Get company details
            string companyName = "";
            string companyEmail = "";
            string companyStatus = "";
            
            var getCompanySql = "SELECT company_name, email, status FROM companies WHERE id = @companyId";
            using (var getCmd = new SqlCommand(getCompanySql, conn))
            {
                getCmd.Parameters.AddWithValue("@companyId", companyId);
                using (var reader = await getCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        companyName = reader.GetString(0);
                        companyEmail = reader.GetString(1);
                        companyStatus = reader.GetString(2);
                    }
                    else
                    {
                        throw new Exception($"Company with ID {companyId} not found");
                    }
                }
            }

            // Check if company is approved
            if (!string.Equals(companyStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Company account is not approved. Current status: {companyStatus}");
            }

            const string sql = @"
                INSERT INTO jobs (
                    title,
                    company_name,
                    company_email,
                    description,
                    location,
                    job_type,
                    category,
                    experience_level,
                    salary_range,
                    salary,
                    requirements,
                    responsibilities,
                    application_deadline,
                    status,
                    is_featured,
                    views_count,
                    posted_at,
                    created_at,
                    company_id,
                    type,
                    deadline
                )
                VALUES (
                    @title,
                    @companyName,
                    @companyEmail,
                    @description,
                    @location,
                    @jobType,
                    @category,
                    @experienceLevel,
                    @salaryRange,
                    @salary,
                    @requirements,
                    @responsibilities,
                    @applicationDeadline,
                    'Active',
                    0,
                    0,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    @companyId,
                    @jobType,
                    @applicationDeadline
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", request.Title.Trim());
            cmd.Parameters.AddWithValue("@companyName", companyName);
            cmd.Parameters.AddWithValue("@companyEmail", companyEmail);
            cmd.Parameters.AddWithValue("@description", request.Description.Trim());
            cmd.Parameters.AddWithValue("@location", request.Location.Trim());
            cmd.Parameters.AddWithValue("@jobType", request.JobType.Trim());
            cmd.Parameters.AddWithValue("@category", request.Category.Trim());
            cmd.Parameters.AddWithValue("@experienceLevel", (object?)request.ExperienceLevel?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@salaryRange", (object?)request.SalaryRange?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@salary", (object?)request.Salary?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@requirements", request.Requirements.Trim());
            cmd.Parameters.AddWithValue("@responsibilities", (object?)request.Responsibilities?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@applicationDeadline", request.ApplicationDeadline.Date);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            var idObj = await cmd.ExecuteScalarAsync();
            return (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);
        }

        /// <summary>
        /// Gets all active jobs posted by a specific company (excludes soft-deleted).
        /// </summary>
        public async Task<List<JobListItemResponse>> GetJobsByCompanyIdAsync(int companyId)
        {
            const string sql = @"
SELECT
    j.id,
    j.title,
    j.requirements,
    j.responsibilities,
    c.company_name,
    j.location,
    j.type,
    j.category,
    j.salary,
    j.deadline
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
WHERE j.company_id = @companyId 
AND (j.is_deleted IS NULL OR j.is_deleted = 0)
ORDER BY j.created_at DESC, j.id DESC;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            var jobs = new List<JobListItemResponse>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                jobs.Add(new JobListItemResponse
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Requirements = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Responsibilities = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CompanyName = reader.GetString(4),
                    Location = reader.GetString(5),
                    Type = reader.GetString(6),
                    Category = reader.GetString(7),
                    Salary = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Deadline = reader.GetDateTime(9)
                });
            }

            return jobs;
        }

        /// <summary>
        /// Gets a single job by ID and verifies it belongs to the specified company (excludes soft-deleted).
        /// </summary>
        public async Task<JobDetailsResponse?> GetJobByCompanyAndIdAsync(int companyId, int jobId)
        {
            const string sql = @"
SELECT
    j.id,
    j.title,
    j.description,
    j.requirements,
    j.responsibilities,
    j.company_id,
    c.company_name,
    j.location,
    j.salary,
    j.type,
    j.category,
    j.deadline,
    j.created_at
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
WHERE j.company_id = @companyId 
AND j.id = @jobId
AND (j.is_deleted IS NULL OR j.is_deleted = 0);
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);
            cmd.Parameters.AddWithValue("@jobId", jobId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new JobDetailsResponse
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Requirements = reader.IsDBNull(3) ? null : reader.GetString(3),
                Responsibilities = reader.IsDBNull(4) ? null : reader.GetString(4),
                CompanyId = reader.GetInt32(5),
                CompanyName = reader.GetString(6),
                Location = reader.GetString(7),
                Salary = reader.IsDBNull(8) ? null : reader.GetString(8),
                Type = reader.GetString(9),
                Category = reader.GetString(10),
                Deadline = reader.GetDateTime(11),
                CreatedAt = reader.GetDateTime(12)
            };
        }

        /// <summary>
        /// Gets job statistics for a company.
        /// Returns active jobs, active internships, and total applicants.
        /// </summary>
        public async Task<CompanyJobStatsResponse> GetJobStatsAsync(int companyId)
        {
            const string sql = @"
SELECT 
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() AND (j.is_deleted IS NULL OR j.is_deleted = 0) THEN 1 END) AS ActiveJobs,
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() AND j.job_type = 'Internship' AND (j.is_deleted IS NULL OR j.is_deleted = 0) THEN 1 END) AS ActiveInternships,
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() AND j.job_type != 'Internship' AND (j.is_deleted IS NULL OR j.is_deleted = 0) THEN 1 END) AS ActiveFullTimeJobs,
    (SELECT COUNT(DISTINCT a.id) 
     FROM applications a 
     INNER JOIN jobs j2 ON a.job_id = j2.id 
     WHERE j2.company_id = @companyId AND (j2.is_deleted IS NULL OR j2.is_deleted = 0)) AS TotalApplicants
FROM jobs j
WHERE j.company_id = @companyId AND (j.is_deleted IS NULL OR j.is_deleted = 0);
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new CompanyJobStatsResponse
            {
                ActiveJobs = reader.GetInt32(0),
                ActiveInternships = reader.GetInt32(1),
                ActiveFullTimeJobs = reader.GetInt32(2),
                TotalApplicants = reader.GetInt32(3)
            };
        }

        /// <summary>
        /// Updates an existing job posting with validation that it belongs to the company.
        /// </summary>
        public async Task<JobDetailsResponse?> UpdateJobAsync(int companyId, int jobId, UpdateJobRequest request)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // First, verify job exists and belongs to the company, and is not soft-deleted
            const string checkSql = @"
                SELECT j.id, j.title, j.company_id, c.company_name
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                WHERE j.id = @jobId 
                AND j.company_id = @companyId 
                AND (j.is_deleted IS NULL OR j.is_deleted = 0)";

            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@jobId", jobId);
                checkCmd.Parameters.AddWithValue("@companyId", companyId);

                using (var reader = await checkCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return null; // Job not found or doesn't belong to company
                    }
                }
            }

            // Validate deadline is in the future (if being updated)
            if (request.ApplicationDeadline.Date <= DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException("Job deadline must be a future date.");
            }

            // Update the job with ALL fields including requirements and responsibilities
            const string updateSql = @"
                UPDATE dbo.jobs
                SET 
                    title = @title,
                    description = @description,
                    requirements = @requirements,
                    responsibilities = @responsibilities,
                    salary = @salary,
                    salary_range = @salaryRange,
                    location = @location,
                    type = @jobType,
                    category = @category,
                    experience_level = @experienceLevel,
                    deadline = @applicationDeadline,
                    updated_at = SYSUTCDATETIME()
                WHERE id = @jobId 
                AND company_id = @companyId
                AND (is_deleted IS NULL OR is_deleted = 0);

                -- Return the updated job details with ALL fields
                SELECT 
                    j.id,
                    j.title,
                    j.description,
                    j.requirements,
                    j.responsibilities,
                    j.company_id,
                    c.company_name,
                    j.location,
                    j.salary,
                    j.type,
                    j.category,
                    j.deadline,
                    j.created_at
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                WHERE j.id = @jobId;";

            using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                updateCmd.Parameters.AddWithValue("@title", request.Title.Trim());
                updateCmd.Parameters.AddWithValue("@description", request.Description.Trim());
                updateCmd.Parameters.AddWithValue("@requirements", request.Requirements.Trim());
                updateCmd.Parameters.AddWithValue("@responsibilities", (object?)request.Responsibilities?.Trim() ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@salary", (object?)request.Salary?.Trim() ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@salaryRange", (object?)request.SalaryRange?.Trim() ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@location", request.Location.Trim());
                updateCmd.Parameters.AddWithValue("@jobType", request.JobType.Trim());
                updateCmd.Parameters.AddWithValue("@category", request.Category.Trim());
                updateCmd.Parameters.AddWithValue("@experienceLevel", (object?)request.ExperienceLevel?.Trim() ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@applicationDeadline", request.ApplicationDeadline.Date);
                updateCmd.Parameters.AddWithValue("@jobId", jobId);
                updateCmd.Parameters.AddWithValue("@companyId", companyId);

                using (var reader = await updateCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return null; // Update failed
                    }

                    return new JobDetailsResponse
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Description = reader.GetString(2),
                        Requirements = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Responsibilities = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CompanyId = reader.GetInt32(5),
                        CompanyName = reader.GetString(6),
                        Location = reader.GetString(7),
                        Salary = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Type = reader.GetString(9),
                        Category = reader.GetString(10),
                        Deadline = reader.GetDateTime(11),
                        CreatedAt = reader.GetDateTime(12)
                    };
                }
            }
        }

        /// <summary>
        /// Soft deletes a job posting (marks as deleted but keeps in database).
        /// </summary>
        public async Task<bool> SoftDeleteJobAsync(int companyId, int jobId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Verify job belongs to company and is not already deleted
            const string checkSql = @"
                SELECT id FROM dbo.jobs 
                WHERE id = @jobId 
                AND company_id = @companyId 
                AND (is_deleted IS NULL OR is_deleted = 0)";

            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@jobId", jobId);
                checkCmd.Parameters.AddWithValue("@companyId", companyId);

                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    return false; // Job not found or doesn't belong to company
                }
            }

            // Soft delete the job
            const string softDeleteSql = @"
                UPDATE dbo.jobs
                SET is_deleted = 1,
                    updated_at = SYSUTCDATETIME(),
                    deleted_at = SYSUTCDATETIME(),
                    status = 'Deleted'
                WHERE id = @jobId 
                AND company_id = @companyId";

            using (var deleteCmd = new SqlCommand(softDeleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@jobId", jobId);
                deleteCmd.Parameters.AddWithValue("@companyId", companyId);

                var rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Hard deletes a job posting permanently from the database.
        /// Use with caution - consider business requirements.
        /// </summary>
        public async Task<bool> HardDeleteJobAsync(int companyId, int jobId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // First check if there are any applications for this job
            const string checkApplicationsSql = @"
                SELECT COUNT(1) FROM dbo.applications 
                WHERE job_id = @jobId";

            using (var checkCmd = new SqlCommand(checkApplicationsSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@jobId", jobId);
                var applicationCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (applicationCount > 0)
                {
                    throw new InvalidOperationException(
                        $"Cannot hard delete job with {applicationCount} existing applications. Consider soft delete instead.");
                }
            }

            // Hard delete the job (only if no applications)
            const string hardDeleteSql = @"
                DELETE FROM dbo.jobs 
                WHERE id = @jobId 
                AND company_id = @companyId";

            using (var deleteCmd = new SqlCommand(hardDeleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@jobId", jobId);
                deleteCmd.Parameters.AddWithValue("@companyId", companyId);

                var rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Gets a single job by ID with ownership verification for editing (excluding soft-deleted).
        /// Returns more details including requirements, responsibilities, etc.
        /// </summary>
        public async Task<JobEditResponse?> GetJobForUpdateAsync(int companyId, int jobId)
        {
            const string sql = @"
                SELECT
                    j.id,
                    j.title,
                    j.description,
                    j.requirements,
                    j.responsibilities,
                    j.company_id,
                    c.company_name,
                    j.location,
                    j.salary,
                    j.salary_range,
                    j.type,
                    j.category,
                    j.experience_level,
                    j.deadline,
                    j.created_at,
                    j.updated_at
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                WHERE j.id = @jobId 
                AND j.company_id = @companyId 
                AND (j.is_deleted IS NULL OR j.is_deleted = 0)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new JobEditResponse
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Requirements = reader.GetString(3),
                Responsibilities = reader.IsDBNull(4) ? null : reader.GetString(4),
                CompanyId = reader.GetInt32(5),
                CompanyName = reader.GetString(6),
                Location = reader.GetString(7),
                Salary = reader.IsDBNull(8) ? null : reader.GetString(8),
                SalaryRange = reader.IsDBNull(9) ? null : reader.GetString(9),
                Type = reader.GetString(10),
                Category = reader.GetString(11),
                ExperienceLevel = reader.IsDBNull(12) ? null : reader.GetString(12),
                Deadline = reader.GetDateTime(13),
                CreatedAt = reader.GetDateTime(14),
                UpdatedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15)
            };
        }
    }

    /// <summary>
    /// DTO for job editing with all fields needed for the edit form
    /// </summary>
    public class JobEditResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Requirements { get; set; } = "";
        public string? Responsibilities { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string? Salary { get; set; }
        public string? SalaryRange { get; set; }
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string? ExperienceLevel { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}