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

        // ========== NEW METHOD - ADD THIS ==========
        /// <summary>
        /// Ensures all AI analytics tables exist (creates them if missing)
        /// Call this during app startup
        /// </summary>
        public async Task EnsureAiTablesExistAsync()
        {
            var sql = @"
            -- Table 1: CV Analysis Results
            IF OBJECT_ID('dbo.cv_analysis_results', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.cv_analysis_results (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    student_id INT NOT NULL,
                    job_id INT NULL,
                    ats_score INT NOT NULL,
                    match_percentage INT NULL,
                    strengths NVARCHAR(MAX) NULL,
                    suggestions NVARCHAR(MAX) NULL,
                    missing_keywords NVARCHAR(MAX) NULL,
                    present_keywords NVARCHAR(MAX) NULL,
                    formatting_feedback NVARCHAR(MAX) NULL,
                    recommendation NVARCHAR(200) NULL,
                    analysis_type NVARCHAR(50) NOT NULL DEFAULT 'Standalone',
                    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    
                    CONSTRAINT FK_cv_analysis_student 
                        FOREIGN KEY (student_id) REFERENCES dbo.students(id) ON DELETE CASCADE,
                    CONSTRAINT FK_cv_analysis_job 
                        FOREIGN KEY (job_id) REFERENCES dbo.jobs(id) ON DELETE SET NULL
                );
                
                CREATE INDEX IX_cv_analysis_student_id ON dbo.cv_analysis_results(student_id);
                CREATE INDEX IX_cv_analysis_job_id ON dbo.cv_analysis_results(job_id);
                CREATE INDEX IX_cv_analysis_created_at ON dbo.cv_analysis_results(created_at);
            END

            -- Table 2: Job Match Analytics
            IF OBJECT_ID('dbo.job_match_analytics', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.job_match_analytics (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    job_id INT NOT NULL,
                    student_id INT NOT NULL,
                    match_score INT NOT NULL,
                    matched_skills NVARCHAR(MAX) NULL,
                    missing_skills NVARCHAR(MAX) NULL,
                    partial_matches NVARCHAR(MAX) NULL,
                    recommendation NVARCHAR(200) NULL,
                    calculated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    
                    CONSTRAINT FK_match_job 
                        FOREIGN KEY (job_id) REFERENCES dbo.jobs(id) ON DELETE CASCADE,
                    CONSTRAINT FK_match_student 
                        FOREIGN KEY (student_id) REFERENCES dbo.students(id) ON DELETE CASCADE
                );
                
                CREATE INDEX IX_job_match_job_id ON dbo.job_match_analytics(job_id);
                CREATE INDEX IX_job_match_student_id ON dbo.job_match_analytics(student_id);
                CREATE INDEX IX_job_match_calculated_at ON dbo.job_match_analytics(calculated_at);
            END

            -- Table 3: Applicant Screening
            IF OBJECT_ID('dbo.applicant_screening', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.applicant_screening (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    application_id INT NOT NULL,
                    screening_score INT NOT NULL,
                    screening_recommendation NVARCHAR(100) NULL,
                    ai_analysis_json NVARCHAR(MAX) NULL,
                    screened_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    
                    CONSTRAINT FK_screening_application 
                        FOREIGN KEY (application_id) REFERENCES dbo.applications(id) ON DELETE CASCADE
                );
                
                CREATE INDEX IX_applicant_screening_application_id ON dbo.applicant_screening(application_id);
                CREATE INDEX IX_applicant_screening_score ON dbo.applicant_screening(screening_score);
            END

            -- Table 4: Analytics History
            IF OBJECT_ID('dbo.analytics_history', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.analytics_history (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    snapshot_date DATE NOT NULL,
                    total_students INT NOT NULL DEFAULT 0,
                    active_companies INT NOT NULL DEFAULT 0,
                    active_jobs INT NOT NULL DEFAULT 0,
                    total_applications INT NOT NULL DEFAULT 0,
                    avg_ats_score DECIMAL(5,2) NULL,
                    avg_match_percentage DECIMAL(5,2) NULL,
                    application_success_rate DECIMAL(5,2) NULL,
                    top_skills NVARCHAR(MAX) NULL,
                    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    
                    CONSTRAINT UQ_analytics_snapshot_date UNIQUE (snapshot_date)
                );
                
                CREATE INDEX IX_analytics_history_snapshot_date ON dbo.analytics_history(snapshot_date);
            END
            ";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
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

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.students WHERE (is_deleted IS NULL OR is_deleted = 0)", conn))
                stats.TotalStudents = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.students WHERE (is_deleted IS NULL OR is_deleted = 0) AND created_at >= DATEADD(DAY, -30, SYSUTCDATETIME())", conn))
                stats.NewStudentsLast30Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.companies WHERE status = 'APPROVED'", conn))
                stats.ActiveCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.jobs WHERE (is_deleted IS NULL OR is_deleted = 0) AND deadline >= CAST(SYSUTCDATETIME() AS DATE)", conn))
                stats.ActiveJobs = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.applications", conn))
                stats.TotalApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.applications WHERE status = 'Accepted'", conn))
                stats.AcceptedApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.companies WHERE status = 'PENDING_APPROVAL' AND created_at <= DATEADD(DAY, -7, SYSUTCDATETIME())", conn))
                stats.StuckPendingCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());

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

        // ========== WRITE METHODS ==========

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

        // ========== UPDATED METHOD - Add retry logic ==========
        public async Task SaveJobMatchAnalyticsAsync(JobMatchAnalytics match)
        {
            const string sql = @"
                INSERT INTO dbo.job_match_analytics 
                (job_id, student_id, match_score, matched_skills, missing_skills, partial_matches, recommendation)
                VALUES (@jobId, @studentId, @matchScore, @matchedSkills, @missingSkills, @partialMatches, @recommendation)";

            try
            {
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
            catch (SqlException ex) when (ex.Number == 208) // Table missing error
            {
                // Try to create tables and retry once
                await EnsureAiTablesExistAsync();
                
                // Retry the insert
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

        // ========== AI ANALYTICS METHODS ==========

        public async Task<PlatformAnalyticsData> GetPlatformAnalyticsForAIAsync()
        {
            var data = new PlatformAnalyticsData();
            
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            // Student engagement metrics - FIXED: CAST to FLOAT
            const string engagementSql = @"
                SELECT 
                    COUNT(DISTINCT s.id) as TotalStudents,
                    COUNT(DISTINCT CASE WHEN a.id IS NOT NULL THEN s.id END) as StudentsWithApplications,
                    CAST(ISNULL(COUNT(DISTINCT CASE WHEN a.id IS NOT NULL THEN s.id END) * 100.0 / NULLIF(COUNT(DISTINCT s.id), 0), 0) AS FLOAT) as EngagementRate
                FROM students s
                LEFT JOIN applications a ON a.student_id = s.id
                WHERE (s.is_deleted IS NULL OR s.is_deleted = 0)";
            
            using (var cmd = new SqlCommand(engagementSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    data.TotalStudents = reader.GetInt32(0);
                    data.StudentsWithApplications = reader.GetInt32(1);
                    data.StudentEngagementRate = reader.GetDouble(2);
                }
            }
            
            // Student trends
            const string trendsSql = @"
                SELECT 
                    YEAR(created_at) as Year,
                    MONTH(created_at) as Month,
                    COUNT(*) as NewStudents
                FROM students
                WHERE created_at >= DATEADD(MONTH, -6, GETDATE())
                    AND (is_deleted IS NULL OR is_deleted = 0)
                GROUP BY YEAR(created_at), MONTH(created_at)
                ORDER BY Year DESC, Month DESC";
            
            using (var cmd = new SqlCommand(trendsSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.StudentTrends.Add(new MonthlyTrend
                    {
                        Year = reader.GetInt32(0),
                        Month = reader.GetInt32(1),
                        Count = reader.GetInt32(2)
                    });
                }
            }
            
            // Job trends by category
            const string jobTrendsSql = @"
                SELECT 
                    category,
                    COUNT(*) as TotalJobs,
                    COUNT(CASE WHEN created_at >= DATEADD(MONTH, -3, GETDATE()) THEN 1 END) as NewJobs
                FROM jobs
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND category IS NOT NULL
                GROUP BY category
                ORDER BY TotalJobs DESC";
            
            using (var cmd = new SqlCommand(jobTrendsSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.JobTrends.Add(new CategoryTrend
                    {
                        Category = reader.GetString(0),
                        TotalJobs = reader.GetInt32(1),
                        NewJobsLast3Months = reader.GetInt32(2)
                    });
                }
            }
            
            // Application status distribution - FIXED: CAST to FLOAT
            const string appStatusSql = @"
                SELECT 
                    status,
                    COUNT(*) as Count,
                    CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS FLOAT) as Percentage
                FROM applications
                GROUP BY status";
            
            using (var cmd = new SqlCommand(appStatusSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.ApplicationStatusDistribution.Add(new StatusDistribution
                    {
                        Status = reader.GetString(0),
                        Count = reader.GetInt32(1),
                        Percentage = reader.GetDouble(2)
                    });
                }
            }
            
            // Top companies
            const string topCompaniesSql = @"
                SELECT TOP 5
                    c.company_name,
                    COUNT(j.id) as JobCount,
                    ISNULL(COUNT(a.id), 0) as TotalApplications
                FROM companies c
                LEFT JOIN jobs j ON j.company_id = c.id AND (j.is_deleted IS NULL OR j.is_deleted = 0)
                LEFT JOIN applications a ON a.job_id = j.id
                WHERE c.status = 'APPROVED'
                GROUP BY c.company_name
                ORDER BY JobCount DESC";
            
            using (var cmd = new SqlCommand(topCompaniesSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.TopCompanies.Add(new CompanyActivity
                    {
                        CompanyName = reader.GetString(0),
                        JobCount = reader.GetInt32(1),
                        TotalApplications = reader.GetInt32(2)
                    });
                }
            }
            
            data.TopSkills = await GetJobSkillDistributionFromDatabaseAsync();
            data.StudentSkills = await GetStudentSkillsDistributionAsync();
            
            return data;
        }

        public async Task<Dictionary<string, int>> GetStudentSkillsDistributionAsync()
        {
            var skills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            const string sql = @"
                SELECT skills, technical_skills
                FROM student_profiles
                WHERE skills IS NOT NULL OR technical_skills IS NOT NULL";
            
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            var commonSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "C#", "JavaScript", "Python", "Java", "SQL", "TypeScript", "React", 
                "Angular", "Vue", "Node.js", ".NET", "Spring Boot", "Django", "Flask",
                "AWS", "Azure", "Docker", "Kubernetes", "Git", "REST API", "GraphQL",
                "MongoDB", "PostgreSQL", "MySQL", "Redis", "Go", "Rust", "Swift"
            };
            
            while (await reader.ReadAsync())
            {
                var allSkills = "";
                if (!reader.IsDBNull(0)) allSkills += reader.GetString(0);
                if (!reader.IsDBNull(1)) allSkills += " " + reader.GetString(1);
                
                foreach (var skill in commonSkills)
                {
                    if (allSkills.Contains(skill, StringComparison.OrdinalIgnoreCase))
                    {
                        if (skills.ContainsKey(skill))
                            skills[skill]++;
                        else
                            skills[skill] = 1;
                    }
                }
            }
            
            return skills;
        }

        // ========== DTO CLASSES ==========

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

    // ========== EXTERNAL DTO CLASSES ==========

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

    public class PlatformAnalyticsData
    {
        public int TotalStudents { get; set; }
        public int StudentsWithApplications { get; set; }
        public double StudentEngagementRate { get; set; }
        public List<MonthlyTrend> StudentTrends { get; set; } = new();
        public List<CategoryTrend> JobTrends { get; set; } = new();
        public List<StatusDistribution> ApplicationStatusDistribution { get; set; } = new();
        public List<CompanyActivity> TopCompanies { get; set; } = new();
        public Dictionary<string, int> TopSkills { get; set; } = new();
        public Dictionary<string, int> StudentSkills { get; set; } = new();
    }

    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
    }

    public class CategoryTrend
    {
        public string Category { get; set; } = "";
        public int TotalJobs { get; set; }
        public int NewJobsLast3Months { get; set; }
    }

    public class StatusDistribution
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class CompanyActivity
    {
        public string CompanyName { get; set; } = "";
        public int JobCount { get; set; }
        public int TotalApplications { get; set; }
    }
}
