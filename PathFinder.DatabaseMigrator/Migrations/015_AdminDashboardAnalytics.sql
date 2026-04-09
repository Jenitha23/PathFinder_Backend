-- =============================================
-- Migration: Admin Dashboard Analytics Indexes
-- Date: 2026-04-09
-- Description: Adds indexes for dashboard analytics performance
-- =============================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- Index for date range filtering on jobs
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_jobs_created_at_analytics' AND object_id = OBJECT_ID('dbo.jobs'))
    BEGIN
        CREATE INDEX IX_jobs_created_at_analytics ON dbo.jobs(created_at) 
        INCLUDE (type, is_deleted);
        PRINT 'Created index IX_jobs_created_at_analytics';
    END
    ELSE
        PRINT 'Index IX_jobs_created_at_analytics already exists';

    -- Index for date range filtering on applications
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_applications_applied_date_analytics' AND object_id = OBJECT_ID('dbo.applications'))
    BEGIN
        CREATE INDEX IX_applications_applied_date_analytics ON dbo.applications(applied_date) 
        INCLUDE (status, job_id);
        PRINT 'Created index IX_applications_applied_date_analytics';
    END
    ELSE
        PRINT 'Index IX_applications_applied_date_analytics already exists';

    -- Index for applications per job query
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_applications_job_status' AND object_id = OBJECT_ID('dbo.applications'))
    BEGIN
        CREATE INDEX IX_applications_job_status ON dbo.applications(job_id, status);
        PRINT 'Created index IX_applications_job_status';
    END
    ELSE
        PRINT 'Index IX_applications_job_status already exists';

    -- Index for student count queries
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_students_created_at' AND object_id = OBJECT_ID('dbo.students'))
    BEGIN
        CREATE INDEX IX_students_created_at ON dbo.students(created_at) 
        INCLUDE (is_deleted);
        PRINT 'Created index IX_students_created_at';
    END
    ELSE
        PRINT 'Index IX_students_created_at already exists';

    -- Index for company count queries
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_companies_created_at_status' AND object_id = OBJECT_ID('dbo.companies'))
    BEGIN
        CREATE INDEX IX_companies_created_at_status ON dbo.companies(created_at, status);
        PRINT 'Created index IX_companies_created_at_status';
    END
    ELSE
        PRINT 'Index IX_companies_created_at_status already exists';

    -- Index for jobs deadline queries (for expiring soon)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_jobs_deadline' AND object_id = OBJECT_ID('dbo.jobs'))
    BEGIN
        CREATE INDEX IX_jobs_deadline ON dbo.jobs(deadline) 
        INCLUDE (is_deleted, status);
        PRINT 'Created index IX_jobs_deadline';
    END
    ELSE
        PRINT 'Index IX_jobs_deadline already exists';

    COMMIT TRANSACTION;
    PRINT 'Admin Dashboard Analytics migration completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH