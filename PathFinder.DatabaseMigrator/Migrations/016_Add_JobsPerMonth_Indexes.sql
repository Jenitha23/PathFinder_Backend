-- =============================================
-- Migration: Jobs Per Month Report Indexes
-- Date: 2026-04-09
-- Description: Adds indexes for jobs per month reporting performance
-- =============================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- Index for date range filtering on jobs (for admin report)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_jobs_created_at_year_month' AND object_id = OBJECT_ID('dbo.jobs'))
    BEGIN
        CREATE INDEX IX_jobs_created_at_year_month ON dbo.jobs(created_at) 
        INCLUDE (type, status, is_deleted, company_id);
        PRINT 'Created index IX_jobs_created_at_year_month';
    END
    ELSE
        PRINT 'Index IX_jobs_created_at_year_month already exists';

    -- Index for company-specific job queries
    IF NOT EXISTS (SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_jobs_company_created_at' AND object_id = OBJECT_ID('dbo.jobs'))
    BEGIN
        CREATE INDEX IX_jobs_company_created_at ON dbo.jobs(company_id, created_at) 
        INCLUDE (type, status, is_deleted);
        PRINT 'Created index IX_jobs_company_created_at';
    END
    ELSE
        PRINT 'Index IX_jobs_company_created_at already exists';

    COMMIT TRANSACTION;
    PRINT 'Jobs per month report indexes migration completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH