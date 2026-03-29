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
        /// Gets all jobs posted by a specific company.
        /// </summary>
        public async Task<List<JobListItemResponse>> GetJobsByCompanyIdAsync(int companyId)
        {
            const string sql = @"
SELECT
    j.id,
    j.title,
    c.company_name,
    j.location,
    j.type,
    j.category,
    j.salary,
    j.deadline
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
WHERE j.company_id = @companyId
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
                    CompanyName = reader.GetString(2),
                    Location = reader.GetString(3),
                    Type = reader.GetString(4),
                    Category = reader.GetString(5),
                    Salary = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Deadline = reader.GetDateTime(7)
                });
            }

            return jobs;
        }

        /// <summary>
        /// Gets a single job by ID and verifies it belongs to the specified company.
        /// </summary>
        public async Task<JobDetailsResponse?> GetJobByCompanyAndIdAsync(int companyId, int jobId)
        {
            const string sql = @"
SELECT
    j.id,
    j.title,
    j.description,
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
WHERE j.company_id = @companyId AND j.id = @jobId;
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
                CompanyId = reader.GetInt32(3),
                CompanyName = reader.GetString(4),
                Location = reader.GetString(5),
                Salary = reader.IsDBNull(6) ? null : reader.GetString(6),
                Type = reader.GetString(7),
                Category = reader.GetString(8),
                Deadline = reader.GetDateTime(9),
                CreatedAt = reader.GetDateTime(10)
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
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() THEN 1 END) AS ActiveJobs,
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() AND j.job_type = 'Internship' THEN 1 END) AS ActiveInternships,
    COUNT(CASE WHEN j.status = 'Active' AND j.deadline >= GETDATE() AND j.job_type != 'Internship' THEN 1 END) AS ActiveFullTimeJobs,
    (SELECT COUNT(DISTINCT a.id) 
     FROM applications a 
     INNER JOIN jobs j2 ON a.job_id = j2.id 
     WHERE j2.company_id = @companyId) AS TotalApplicants
FROM jobs j
WHERE j.company_id = @companyId;
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
    }
}