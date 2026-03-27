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
    }
}