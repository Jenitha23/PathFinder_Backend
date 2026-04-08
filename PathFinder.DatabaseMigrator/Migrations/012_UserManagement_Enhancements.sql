-- Add soft delete column to jobs table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'jobs' AND COLUMN_NAME = 'is_deleted')
BEGIN
    ALTER TABLE dbo.jobs ADD is_deleted BIT NOT NULL DEFAULT 0;
    PRINT 'Added is_deleted column for soft delete';
END
ELSE
    PRINT 'is_deleted column already exists';

-- Add index for better performance on deleted jobs filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_jobs_is_deleted' AND object_id = OBJECT_ID('dbo.jobs'))
BEGIN
    CREATE INDEX IX_jobs_is_deleted ON dbo.jobs(is_deleted);
    PRINT 'Created index on is_deleted';
END
ELSE
    PRINT 'Index IX_jobs_is_deleted already exists';

-- Add updated_at column for tracking changes
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'jobs' AND COLUMN_NAME = 'updated_at')
BEGIN
    ALTER TABLE dbo.jobs ADD updated_at DATETIME2 NULL;
    PRINT 'Added updated_at column';
END
ELSE
    PRINT 'updated_at column already exists';

-- Update existing jobs to have updated_at = created_at (only if updated_at is NULL)
UPDATE dbo.jobs SET updated_at = created_at WHERE updated_at IS NULL AND created_at IS NOT NULL;
PRINT 'Updated existing jobs with updated_at values';