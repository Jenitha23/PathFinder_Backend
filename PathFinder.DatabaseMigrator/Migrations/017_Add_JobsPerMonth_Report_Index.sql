-- =============================================
-- Migration: Add index for jobs per month report
-- Date: 2026-04-09
-- =============================================

-- Index to speed up date range + company filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_jobs_company_created_month' AND object_id = OBJECT_ID('dbo.jobs'))
BEGIN
    CREATE INDEX IX_jobs_company_created_month 
    ON dbo.jobs(company_id, created_at) 
    INCLUDE (is_deleted);
    PRINT 'Created index IX_jobs_company_created_month';
END

-- Index for admin report (no company filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_jobs_created_month' AND object_id = OBJECT_ID('dbo.jobs'))
BEGIN
    CREATE INDEX IX_jobs_created_month 
    ON dbo.jobs(created_at) 
    INCLUDE (is_deleted);
    PRINT 'Created index IX_jobs_created_month';
END