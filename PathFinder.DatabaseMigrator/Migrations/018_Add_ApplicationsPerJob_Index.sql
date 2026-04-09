-- =============================================
-- Migration: Add indexes for applications per job report
-- Date: 2026-04-09
-- =============================================

-- Index for filtering applications by applied_date
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_applications_applied_date_report' AND object_id = OBJECT_ID('dbo.applications'))
BEGIN
    CREATE INDEX IX_applications_applied_date_report 
    ON dbo.applications(applied_date) 
    INCLUDE (job_id, status);
    PRINT 'Created index IX_applications_applied_date_report';
END

-- Composite index for joining jobs with companies and filtering by job_id
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_applications_job_status_date' AND object_id = OBJECT_ID('dbo.applications'))
BEGIN
    CREATE INDEX IX_applications_job_status_date 
    ON dbo.applications(job_id, status, applied_date);
    PRINT 'Created index IX_applications_job_status_date';
END