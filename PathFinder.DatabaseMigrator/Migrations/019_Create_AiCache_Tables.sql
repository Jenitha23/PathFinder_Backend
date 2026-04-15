-- =============================================
-- Migration: AI Analytics Tables
-- Description: Stores CV analysis, job matches, applicant screening, and analytics history
-- Date: 2024-01-15
-- =============================================

BEGIN TRY
    BEGIN TRANSACTION;

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
            analysis_type NVARCHAR(50) NOT NULL,
            created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT FK_cv_analysis_student FOREIGN KEY (student_id) REFERENCES dbo.students(id),
            CONSTRAINT FK_cv_analysis_job FOREIGN KEY (job_id) REFERENCES dbo.jobs(id),
            CONSTRAINT CK_analysis_type CHECK (analysis_type IN ('Standalone', 'JobSpecific'))
        );
        
        CREATE INDEX IX_cv_analysis_student_id ON dbo.cv_analysis_results(student_id);
        CREATE INDEX IX_cv_analysis_job_id ON dbo.cv_analysis_results(job_id);
        CREATE INDEX IX_cv_analysis_created_at ON dbo.cv_analysis_results(created_at);
        
        PRINT '✅ Created cv_analysis_results table';
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
            
            CONSTRAINT FK_match_job FOREIGN KEY (job_id) REFERENCES dbo.jobs(id),
            CONSTRAINT FK_match_student FOREIGN KEY (student_id) REFERENCES dbo.students(id)
        );
        
        CREATE INDEX IX_job_match_job_id ON dbo.job_match_analytics(job_id);
        CREATE INDEX IX_job_match_student_id ON dbo.job_match_analytics(student_id);
        CREATE INDEX IX_job_match_calculated_at ON dbo.job_match_analytics(calculated_at);
        
        PRINT '✅ Created job_match_analytics table';
    END

    -- Table 3: Applicant Screening Results
    IF OBJECT_ID('dbo.applicant_screening', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.applicant_screening (
            id INT IDENTITY(1,1) PRIMARY KEY,
            application_id INT NOT NULL,
            screening_score INT NOT NULL,
            screening_recommendation NVARCHAR(100) NULL,
            ai_analysis_json NVARCHAR(MAX) NULL,
            screened_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT FK_screening_application FOREIGN KEY (application_id) REFERENCES dbo.applications(id) ON DELETE CASCADE
        );
        
        CREATE INDEX IX_applicant_screening_application_id ON dbo.applicant_screening(application_id);
        CREATE INDEX IX_applicant_screening_score ON dbo.applicant_screening(screening_score);
        
        PRINT '✅ Created applicant_screening table';
    END

    -- Table 4: Analytics History (for trends and reporting)
    IF OBJECT_ID('dbo.analytics_history', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.analytics_history (
            id INT IDENTITY(1,1) PRIMARY KEY,
            snapshot_date DATE NOT NULL,
            total_students INT NOT NULL,
            active_companies INT NOT NULL,
            active_jobs INT NOT NULL,
            total_applications INT NOT NULL,
            avg_ats_score DECIMAL(5,2) NULL,
            avg_match_percentage DECIMAL(5,2) NULL,
            application_success_rate DECIMAL(5,2) NULL,
            top_skills NVARCHAR(MAX) NULL,
            created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT UQ_analytics_snapshot_date UNIQUE (snapshot_date)
        );
        
        CREATE INDEX IX_analytics_history_snapshot_date ON dbo.analytics_history(snapshot_date);
        
        PRINT '✅ Created analytics_history table';
    END

    COMMIT TRANSACTION;
    PRINT '✅ AI Analytics migration completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH