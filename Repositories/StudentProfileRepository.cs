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

        // ✅ Create table if not exists + add missing columns (SQL Server safe migration)
        public async Task EnsureTableAndColumnsAsync()
        {
            var sql = @"
IF OBJECT_ID('dbo.student_profiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_profiles (
        student_id INT NOT NULL PRIMARY KEY,

        -- Basic profile
        headline NVARCHAR(150) NULL,
        about_me NVARCHAR(MAX) NULL,

        -- Existing fields
        skills NVARCHAR(MAX) NULL,
        education NVARCHAR(MAX) NULL,
        experience NVARCHAR(MAX) NULL,

        -- Contact
        phone NVARCHAR(30) NULL,
        address NVARCHAR(300) NULL,
        city NVARCHAR(100) NULL,
        country NVARCHAR(100) NULL,

        -- Education structured
        university NVARCHAR(150) NULL,
        degree NVARCHAR(150) NULL,
        academic_year NVARCHAR(50) NULL,
        gpa NVARCHAR(20) NULL,

        -- Skill categories
        technical_skills NVARCHAR(MAX) NULL,
        soft_skills NVARCHAR(MAX) NULL,
        languages NVARCHAR(MAX) NULL,

        -- Career preferences
        career_interests NVARCHAR(MAX) NULL,
        preferred_job_type NVARCHAR(50) NULL,
        work_mode NVARCHAR(50) NULL,
        available_from DATE NULL,

        -- Links
        github_url NVARCHAR(300) NULL,
        linkedin_url NVARCHAR(300) NULL,
        portfolio_url NVARCHAR(300) NULL,

        -- Extra info
        projects_summary NVARCHAR(MAX) NULL,
        internship_experience NVARCHAR(MAX) NULL,
        certifications NVARCHAR(MAX) NULL,

        -- CV
        cv_url NVARCHAR(500) NULL,
        updated_at_utc DATETIME2 NULL,

        CONSTRAINT FK_student_profiles_students
            FOREIGN KEY (student_id) REFERENCES dbo.students(id)
            ON DELETE CASCADE
    );
END;

-- ✅ Add missing columns safely if table already exists
IF COL_LENGTH('dbo.student_profiles', 'headline') IS NULL ALTER TABLE dbo.student_profiles ADD headline NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.student_profiles', 'about_me') IS NULL ALTER TABLE dbo.student_profiles ADD about_me NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'skills') IS NULL ALTER TABLE dbo.student_profiles ADD skills NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'education') IS NULL ALTER TABLE dbo.student_profiles ADD education NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'experience') IS NULL ALTER TABLE dbo.student_profiles ADD experience NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'phone') IS NULL ALTER TABLE dbo.student_profiles ADD phone NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.student_profiles', 'address') IS NULL ALTER TABLE dbo.student_profiles ADD address NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.student_profiles', 'city') IS NULL ALTER TABLE dbo.student_profiles ADD city NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.student_profiles', 'country') IS NULL ALTER TABLE dbo.student_profiles ADD country NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.student_profiles', 'university') IS NULL ALTER TABLE dbo.student_profiles ADD university NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.student_profiles', 'degree') IS NULL ALTER TABLE dbo.student_profiles ADD degree NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.student_profiles', 'academic_year') IS NULL ALTER TABLE dbo.student_profiles ADD academic_year NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.student_profiles', 'gpa') IS NULL ALTER TABLE dbo.student_profiles ADD gpa NVARCHAR(20) NULL;

IF COL_LENGTH('dbo.student_profiles', 'technical_skills') IS NULL ALTER TABLE dbo.student_profiles ADD technical_skills NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'soft_skills') IS NULL ALTER TABLE dbo.student_profiles ADD soft_skills NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'languages') IS NULL ALTER TABLE dbo.student_profiles ADD languages NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'career_interests') IS NULL ALTER TABLE dbo.student_profiles ADD career_interests NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'preferred_job_type') IS NULL ALTER TABLE dbo.student_profiles ADD preferred_job_type NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.student_profiles', 'work_mode') IS NULL ALTER TABLE dbo.student_profiles ADD work_mode NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.student_profiles', 'available_from') IS NULL ALTER TABLE dbo.student_profiles ADD available_from DATE NULL;

IF COL_LENGTH('dbo.student_profiles', 'github_url') IS NULL ALTER TABLE dbo.student_profiles ADD github_url NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.student_profiles', 'linkedin_url') IS NULL ALTER TABLE dbo.student_profiles ADD linkedin_url NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.student_profiles', 'portfolio_url') IS NULL ALTER TABLE dbo.student_profiles ADD portfolio_url NVARCHAR(300) NULL;

IF COL_LENGTH('dbo.student_profiles', 'projects_summary') IS NULL ALTER TABLE dbo.student_profiles ADD projects_summary NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'internship_experience') IS NULL ALTER TABLE dbo.student_profiles ADD internship_experience NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.student_profiles', 'certifications') IS NULL ALTER TABLE dbo.student_profiles ADD certifications NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'cv_url') IS NULL ALTER TABLE dbo.student_profiles ADD cv_url NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.student_profiles', 'updated_at_utc') IS NULL ALTER TABLE dbo.student_profiles ADD updated_at_utc DATETIME2 NULL;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<StudentProfileResponse?> GetStudentProfileAsync(int studentId)
        {
            // NOTE: StudentProfileResponse must contain these fields too.
            var sql = @"
SELECT 
    s.id,
    s.full_name,
    s.email,

    p.headline,
    p.about_me,

    p.skills,
    p.education,
    p.experience,

    p.phone,
    p.address,
    p.city,
    p.country,

    p.university,
    p.degree,
    p.academic_year,
    p.gpa,

    p.technical_skills,
    p.soft_skills,
    p.languages,

    p.career_interests,
    p.preferred_job_type,
    p.work_mode,
    p.available_from,

    p.github_url,
    p.linkedin_url,
    p.portfolio_url,

    p.projects_summary,
    p.internship_experience,
    p.certifications,

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

            // Keep safe null checks
            string? GetStr(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
            DateTime? GetDt(int i) => reader.IsDBNull(i) ? null : reader.GetDateTime(i);
            DateTime? GetDateOnlyAsDateTime(int i) => reader.IsDBNull(i) ? null : reader.GetDateTime(i);

            return new StudentProfileResponse
            {
                StudentId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),

                Headline = GetStr(3),
                AboutMe = GetStr(4),

                Skills = GetStr(5),
                Education = GetStr(6),
                Experience = GetStr(7),

                Phone = GetStr(8),
                Address = GetStr(9),
                City = GetStr(10),
                Country = GetStr(11),

                University = GetStr(12),
                Degree = GetStr(13),
                AcademicYear = GetStr(14),
                Gpa = GetStr(15),

                TechnicalSkills = GetStr(16),
                SoftSkills = GetStr(17),
                Languages = GetStr(18),

                CareerInterests = GetStr(19),
                PreferredJobType = GetStr(20),
                WorkMode = GetStr(21),
                AvailableFrom = GetDateOnlyAsDateTime(22),

                GithubUrl = GetStr(23),
                LinkedinUrl = GetStr(24),
                PortfolioUrl = GetStr(25),

                ProjectsSummary = GetStr(26),
                InternshipExperience = GetStr(27),
                Certifications = GetStr(28),

                CvUrl = GetStr(29),
                UpdatedAtUtc = GetDt(30)
            };
        }

        // ✅ Upsert profile row (insert or update) with all fields
        public async Task UpsertStudentProfileAsync(int studentId, StudentProfileUpdateRequest req, string? cvUrl)
        {
            var sql = @"
IF EXISTS (SELECT 1 FROM dbo.student_profiles WHERE student_id = @studentId)
BEGIN
    UPDATE dbo.student_profiles
    SET 
        headline = @headline,
        about_me = @about_me,

        skills = @skills,
        education = @education,
        experience = @experience,

        phone = @phone,
        address = @address,
        city = @city,
        country = @country,

        university = @university,
        degree = @degree,
        academic_year = @academic_year,
        gpa = @gpa,

        technical_skills = @technical_skills,
        soft_skills = @soft_skills,
        languages = @languages,

        career_interests = @career_interests,
        preferred_job_type = @preferred_job_type,
        work_mode = @work_mode,
        available_from = @available_from,

        github_url = @github_url,
        linkedin_url = @linkedin_url,
        portfolio_url = @portfolio_url,

        projects_summary = @projects_summary,
        internship_experience = @internship_experience,
        certifications = @certifications,

        cv_url = COALESCE(@cv_url, cv_url),
        updated_at_utc = SYSUTCDATETIME()
    WHERE student_id = @studentId;
END
ELSE
BEGIN
    INSERT INTO dbo.student_profiles (
        student_id,
        headline, about_me,
        skills, education, experience,
        phone, address, city, country,
        university, degree, academic_year, gpa,
        technical_skills, soft_skills, languages,
        career_interests, preferred_job_type, work_mode, available_from,
        github_url, linkedin_url, portfolio_url,
        projects_summary, internship_experience, certifications,
        cv_url, updated_at_utc
    )
    VALUES (
        @studentId,
        @headline, @about_me,
        @skills, @education, @experience,
        @phone, @address, @city, @country,
        @university, @degree, @academic_year, @gpa,
        @technical_skills, @soft_skills, @languages,
        @career_interests, @preferred_job_type, @work_mode, @available_from,
        @github_url, @linkedin_url, @portfolio_url,
        @projects_summary, @internship_experience, @certifications,
        @cv_url, SYSUTCDATETIME()
    );
END
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@studentId", studentId);

            object DbVal(string? s) => (object?)s ?? DBNull.Value;

            cmd.Parameters.AddWithValue("@headline", DbVal(req.Headline?.Trim()));
            cmd.Parameters.AddWithValue("@about_me", DbVal(req.AboutMe?.Trim()));

            cmd.Parameters.AddWithValue("@skills", DbVal(req.Skills?.Trim()));
            cmd.Parameters.AddWithValue("@education", DbVal(req.Education?.Trim()));
            cmd.Parameters.AddWithValue("@experience", DbVal(req.Experience?.Trim()));

            cmd.Parameters.AddWithValue("@phone", DbVal(req.Phone?.Trim()));
            cmd.Parameters.AddWithValue("@address", DbVal(req.Address?.Trim()));
            cmd.Parameters.AddWithValue("@city", DbVal(req.City?.Trim()));
            cmd.Parameters.AddWithValue("@country", DbVal(req.Country?.Trim()));

            cmd.Parameters.AddWithValue("@university", DbVal(req.University?.Trim()));
            cmd.Parameters.AddWithValue("@degree", DbVal(req.Degree?.Trim()));
            cmd.Parameters.AddWithValue("@academic_year", DbVal(req.AcademicYear?.Trim()));
            cmd.Parameters.AddWithValue("@gpa", DbVal(req.Gpa?.Trim()));

            cmd.Parameters.AddWithValue("@technical_skills", DbVal(req.TechnicalSkills?.Trim()));
            cmd.Parameters.AddWithValue("@soft_skills", DbVal(req.SoftSkills?.Trim()));
            cmd.Parameters.AddWithValue("@languages", DbVal(req.Languages?.Trim()));

            cmd.Parameters.AddWithValue("@career_interests", DbVal(req.CareerInterests?.Trim()));
            cmd.Parameters.AddWithValue("@preferred_job_type", DbVal(req.PreferredJobType?.Trim()));
            cmd.Parameters.AddWithValue("@work_mode", DbVal(req.WorkMode?.Trim()));
            cmd.Parameters.AddWithValue("@available_from", (object?)req.AvailableFrom ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@github_url", DbVal(req.GithubUrl?.Trim()));
            cmd.Parameters.AddWithValue("@linkedin_url", DbVal(req.LinkedinUrl?.Trim()));
            cmd.Parameters.AddWithValue("@portfolio_url", DbVal(req.PortfolioUrl?.Trim()));

            cmd.Parameters.AddWithValue("@projects_summary", DbVal(req.ProjectsSummary?.Trim()));
            cmd.Parameters.AddWithValue("@internship_experience", DbVal(req.InternshipExperience?.Trim()));
            cmd.Parameters.AddWithValue("@certifications", DbVal(req.Certifications?.Trim()));

            cmd.Parameters.AddWithValue("@cv_url", (object?)cvUrl ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}