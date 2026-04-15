using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;
using System.Text.Json;

namespace PATHFINDER_BACKEND.Repositories
{
    public class AiAnalyticsRepository
    {
        private readonly Db _db;

        public AiAnalyticsRepository(Db db)
        {
            _db = db;
        }

        // ========== READ METHODS ==========

        public async Task<(string? CvUrl, string? Skills, string? TechnicalSkills, string? Education, string? University, string? Degree)> 
            GetStudentProfileForAnalysisAsync(int studentId)
        {
            const string sql = @"
                SELECT sp.cv_url, sp.skills, sp.technical_skills, sp.education, sp.university, sp.degree
                FROM dbo.students s
                LEFT JOIN dbo.student_profiles sp ON sp.student_id = s.id
                WHERE s.id = @studentId AND (s.is_deleted IS NULL OR s.is_deleted = 0)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.IsDBNull(0) ? null : reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5)
                );
            }
            return (null, null, null, null, null, null);
        }

        public async Task<List<JobInfo>> GetActiveJobsAsync()
        {
            const string sql = @"
                SELECT j.id, j.title, j.description, COALESCE(j.requirements, ''), c.company_name, c.id as company_id
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                WHERE (j.is_deleted IS NULL OR j.is_deleted = 0)
                    AND j.deadline >= CAST(SYSUTCDATETIME() AS DATE)
                    AND c.status = 'APPROVED'
                ORDER BY j.created_at DESC";

            var jobs = new List<JobInfo>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                jobs.Add(new JobInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Requirements = reader.GetString(3),
                    CompanyName = reader.GetString(4),
                    CompanyId = reader.GetInt32(5)
                });
            }
            return jobs;
        }

        public async Task<JobInfo?> GetJobByIdAsync(int jobId)
        {
            const string sql = @"
                SELECT j.id, j.title, j.description, COALESCE(j.requirements, ''), c.company_name, c.id
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                WHERE j.id = @jobId AND (j.is_deleted IS NULL OR j.is_deleted = 0)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new JobInfo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Requirements = reader.GetString(3),
                    CompanyName = reader.GetString(4),
                    CompanyId = reader.GetInt32(5)
                };
            }
            return null;
        }

        public async Task<List<ApplicantInfo>> GetApplicantsForJobAsync(int jobId, int companyId)
        {
            const string sql = @"
                SELECT a.id, s.id, s.full_name, s.email, sp.cv_url, sp.skills, a.status, a.applied_date
                FROM dbo.applications a
                INNER JOIN dbo.students s ON a.student_id = s.id
                LEFT JOIN dbo.student_profiles sp ON sp.student_id = s.id
                INNER JOIN dbo.jobs j ON a.job_id = j.id
                WHERE a.job_id = @jobId AND j.company_id = @companyId
                    AND (s.is_deleted IS NULL OR s.is_deleted = 0)
                ORDER BY a.applied_date DESC";

            var applicants = new List<ApplicantInfo>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@jobId", jobId);
            cmd.Parameters.AddWithValue("@companyId", companyId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                applicants.Add(new ApplicantInfo
                {
                    ApplicationId = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    StudentName = reader.GetString(2),
                    StudentEmail = reader.GetString(3),
                    CvUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Skills = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Status = reader.GetString(6),
                    AppliedDate = reader.GetDateTime(7)
                });
            }
            return applicants;
        }

        /// <summary>
        /// Get REAL skill distribution from job requirements (not hardcoded)
        /// </summary>
        public async Task<Dictionary<string, int>> GetJobSkillDistributionFromDatabaseAsync()
        {
            var skills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            const string sql = @"
                SELECT requirements
                FROM dbo.jobs
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND requirements IS NOT NULL";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var commonSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "C#", "JavaScript", "Python", "Java", "SQL", "TypeScript", "React", 
                "Angular", "Vue", "Node.js", ".NET", "Spring Boot", "Django", "Flask",
                "AWS", "Azure", "Docker", "Kubernetes", "Git", "REST API", "GraphQL",
                "MongoDB", "PostgreSQL", "MySQL", "Redis", "Entity Framework", "LINQ",
                "HTML", "CSS", "Bootstrap", "Tailwind", "jQuery", "PHP", "Ruby", "Go",
                "Rust", "Swift", "Kotlin", "Flutter", "React Native", "Xamarin"
            };

            while (await reader.ReadAsync())
            {
                var requirements = reader.GetString(0);
                if (string.IsNullOrEmpty(requirements)) continue;

                foreach (var skill in commonSkills)
                {
                    if (requirements.Contains(skill, StringComparison.OrdinalIgnoreCase))
                    {
                        if (skills.ContainsKey(skill))
                            skills[skill]++;
                        else
                            skills[skill] = 1;
                    }
                }
            }

            return skills.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<PlatformStats> GetPlatformStatsAsync()
        {
            var stats = new PlatformStats();

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Total Students
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.students WHERE (is_deleted IS NULL OR is_deleted = 0)", conn))
                stats.TotalStudents = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // New Students (Last 30 Days)
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.students WHERE (is_deleted IS NULL OR is_deleted = 0) AND created_at >= DATEADD(DAY, -30, SYSUTCDATETIME())", conn))
                stats.NewStudentsLast30Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Active Companies
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.companies WHERE status = 'APPROVED'", conn))
                stats.ActiveCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Active Jobs
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.jobs WHERE (is_deleted IS NULL OR is_deleted = 0) AND deadline >= CAST(SYSUTCDATETIME() AS DATE)", conn))
                stats.ActiveJobs = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Total Applications
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.applications", conn))
                stats.TotalApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Accepted Applications
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.applications WHERE status = 'Accepted'", conn))
                stats.AcceptedApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Stuck Pending Companies
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.companies WHERE status = 'PENDING_APPROVAL' AND created_at <= DATEADD(DAY, -7, SYSUTCDATETIME())", conn))
                stats.StuckPendingCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Average Applicants Per Job
            using (var cmd = new SqlCommand(@"
                SELECT ISNULL(AVG(ApplicationCount), 0) FROM (
                    SELECT COUNT(1) AS ApplicationCount FROM dbo.applications GROUP BY job_id
                ) AS JobApplications", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                stats.AverageApplicantsPerJob = result != DBNull.Value ? Convert.ToDouble(result) : 0;
            }

            stats.ApplicationSuccessRate = stats.TotalApplications > 0
                ? (stats.AcceptedApplications / (double)stats.TotalApplications) * 100 : 0;

            return stats;
        }

        // ========== WRITE METHODS (Storage) ==========

        public async Task SaveCvAnalysisResultAsync(CvAnalysisResult result)
        {
            const string sql = @"
                INSERT INTO dbo.cv_analysis_results 
                (student_id, job_id, ats_score, match_percentage, strengths, suggestions, 
                 missing_keywords, present_keywords, formatting_feedback, recommendation, analysis_type)
                VALUES (@studentId, @jobId, @atsScore, @matchPercentage, @strengths, @suggestions,
                        @missingKeywords, @presentKeywords, @formattingFeedback, @recommendation, @analysisType)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", result.StudentId);
            cmd.Parameters.AddWithValue("@jobId", (object?)result.JobId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@atsScore", result.AtsScore);
            cmd.Parameters.AddWithValue("@matchPercentage", (object?)result.MatchPercentage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@strengths", (object?)result.Strengths ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@suggestions", (object?)result.Suggestions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@missingKeywords", (object?)result.MissingKeywords ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@presentKeywords", (object?)result.PresentKeywords ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@formattingFeedback", (object?)result.FormattingFeedback ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@recommendation", (object?)result.Recommendation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@analysisType", result.AnalysisType);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveJobMatchAnalyticsAsync(JobMatchAnalytics match)
        {
            const string sql = @"
                INSERT INTO dbo.job_match_analytics 
                (job_id, student_id, match_score, matched_skills, missing_skills, partial_matches, recommendation)
                VALUES (@jobId, @studentId, @matchScore, @matchedSkills, @missingSkills, @partialMatches, @recommendation)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@jobId", match.JobId);
            cmd.Parameters.AddWithValue("@studentId", match.StudentId);
            cmd.Parameters.AddWithValue("@matchScore", match.MatchScore);
            cmd.Parameters.AddWithValue("@matchedSkills", (object?)match.MatchedSkills ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@missingSkills", (object?)match.MissingSkills ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@partialMatches", (object?)match.PartialMatches ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@recommendation", (object?)match.Recommendation ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveApplicantScreeningAsync(int applicationId, int score, string recommendation, string aiAnalysisJson)
        {
            const string sql = @"
                INSERT INTO dbo.applicant_screening (application_id, screening_score, screening_recommendation, ai_analysis_json)
                VALUES (@applicationId, @score, @recommendation, @aiAnalysis)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@applicationId", applicationId);
            cmd.Parameters.AddWithValue("@score", score);
            cmd.Parameters.AddWithValue("@recommendation", recommendation);
            cmd.Parameters.AddWithValue("@aiAnalysis", aiAnalysisJson);
            await cmd.ExecuteNonQueryAsync();
        }

        // ========== DTO Classes ==========

        public class PlatformStats
        {
            public int TotalStudents { get; set; }
            public int NewStudentsLast30Days { get; set; }
            public int ActiveCompanies { get; set; }
            public int ActiveJobs { get; set; }
            public int TotalApplications { get; set; }
            public int AcceptedApplications { get; set; }
            public int StuckPendingCompanies { get; set; }
            public double AverageApplicantsPerJob { get; set; }
            public double ApplicationSuccessRate { get; set; }
        }
    }

    public class JobInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Requirements { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int CompanyId { get; set; }
    }

    public class ApplicantInfo
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        public string? CvUrl { get; set; }
        public string? Skills { get; set; }
        public string Status { get; set; } = "";
        public DateTime AppliedDate { get; set; }
    }
}