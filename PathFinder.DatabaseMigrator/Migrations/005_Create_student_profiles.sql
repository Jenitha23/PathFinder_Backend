IF OBJECT_ID('dbo.student_profiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_profiles (
        student_id INT NOT NULL PRIMARY KEY,

        headline NVARCHAR(150) NULL,
        about_me NVARCHAR(MAX) NULL,

        skills NVARCHAR(MAX) NULL,
        education NVARCHAR(MAX) NULL,
        experience NVARCHAR(MAX) NULL,

        phone NVARCHAR(30) NULL,
        address NVARCHAR(300) NULL,
        city NVARCHAR(100) NULL,
        country NVARCHAR(100) NULL,

        university NVARCHAR(150) NULL,
        degree NVARCHAR(150) NULL,
        academic_year NVARCHAR(50) NULL,
        gpa NVARCHAR(20) NULL,

        technical_skills NVARCHAR(MAX) NULL,
        soft_skills NVARCHAR(MAX) NULL,
        languages NVARCHAR(MAX) NULL,

        career_interests NVARCHAR(MAX) NULL,
        preferred_job_type NVARCHAR(50) NULL,
        work_mode NVARCHAR(50) NULL,
        available_from DATE NULL,

        github_url NVARCHAR(300) NULL,
        linkedin_url NVARCHAR(300) NULL,
        portfolio_url NVARCHAR(300) NULL,

        projects_summary NVARCHAR(MAX) NULL,
        internship_experience NVARCHAR(MAX) NULL,
        certifications NVARCHAR(MAX) NULL,

        cv_url NVARCHAR(500) NULL,
        updated_at_utc DATETIME2 NULL,

        CONSTRAINT FK_student_profiles_students
            FOREIGN KEY (student_id) REFERENCES dbo.students(id)
            ON DELETE CASCADE
    );
END;

IF COL_LENGTH('dbo.student_profiles', 'headline') IS NULL
    ALTER TABLE dbo.student_profiles ADD headline NVARCHAR(150) NULL;

IF COL_LENGTH('dbo.student_profiles', 'about_me') IS NULL
    ALTER TABLE dbo.student_profiles ADD about_me NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'skills') IS NULL
    ALTER TABLE dbo.student_profiles ADD skills NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'education') IS NULL
    ALTER TABLE dbo.student_profiles ADD education NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'experience') IS NULL
    ALTER TABLE dbo.student_profiles ADD experience NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'phone') IS NULL
    ALTER TABLE dbo.student_profiles ADD phone NVARCHAR(30) NULL;

IF COL_LENGTH('dbo.student_profiles', 'address') IS NULL
    ALTER TABLE dbo.student_profiles ADD address NVARCHAR(300) NULL;

IF COL_LENGTH('dbo.student_profiles', 'city') IS NULL
    ALTER TABLE dbo.student_profiles ADD city NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.student_profiles', 'country') IS NULL
    ALTER TABLE dbo.student_profiles ADD country NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.student_profiles', 'university') IS NULL
    ALTER TABLE dbo.student_profiles ADD university NVARCHAR(150) NULL;

IF COL_LENGTH('dbo.student_profiles', 'degree') IS NULL
    ALTER TABLE dbo.student_profiles ADD degree NVARCHAR(150) NULL;

IF COL_LENGTH('dbo.student_profiles', 'academic_year') IS NULL
    ALTER TABLE dbo.student_profiles ADD academic_year NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.student_profiles', 'gpa') IS NULL
    ALTER TABLE dbo.student_profiles ADD gpa NVARCHAR(20) NULL;

IF COL_LENGTH('dbo.student_profiles', 'technical_skills') IS NULL
    ALTER TABLE dbo.student_profiles ADD technical_skills NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'soft_skills') IS NULL
    ALTER TABLE dbo.student_profiles ADD soft_skills NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'languages') IS NULL
    ALTER TABLE dbo.student_profiles ADD languages NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'career_interests') IS NULL
    ALTER TABLE dbo.student_profiles ADD career_interests NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'preferred_job_type') IS NULL
    ALTER TABLE dbo.student_profiles ADD preferred_job_type NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.student_profiles', 'work_mode') IS NULL
    ALTER TABLE dbo.student_profiles ADD work_mode NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.student_profiles', 'available_from') IS NULL
    ALTER TABLE dbo.student_profiles ADD available_from DATE NULL;

IF COL_LENGTH('dbo.student_profiles', 'github_url') IS NULL
    ALTER TABLE dbo.student_profiles ADD github_url NVARCHAR(300) NULL;

IF COL_LENGTH('dbo.student_profiles', 'linkedin_url') IS NULL
    ALTER TABLE dbo.student_profiles ADD linkedin_url NVARCHAR(300) NULL;

IF COL_LENGTH('dbo.student_profiles', 'portfolio_url') IS NULL
    ALTER TABLE dbo.student_profiles ADD portfolio_url NVARCHAR(300) NULL;

IF COL_LENGTH('dbo.student_profiles', 'projects_summary') IS NULL
    ALTER TABLE dbo.student_profiles ADD projects_summary NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'internship_experience') IS NULL
    ALTER TABLE dbo.student_profiles ADD internship_experience NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'certifications') IS NULL
    ALTER TABLE dbo.student_profiles ADD certifications NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.student_profiles', 'cv_url') IS NULL
    ALTER TABLE dbo.student_profiles ADD cv_url NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.student_profiles', 'updated_at_utc') IS NULL
    ALTER TABLE dbo.student_profiles ADD updated_at_utc DATETIME2 NULL;