-- Check if column exists first
IF COL_LENGTH('dbo.companies', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD is_deleted BIT NOT NULL DEFAULT 0;
    PRINT '✅ Added is_deleted column';
END
ELSE
    PRINT 'is_deleted column already exists';

-- Add deleted_at column
IF COL_LENGTH('dbo.companies', 'deleted_at') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD deleted_at DATETIME2 NULL;
    PRINT '✅ Added deleted_at column';
END

-- Add updated_at column
IF COL_LENGTH('dbo.companies', 'updated_at') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD updated_at DATETIME2 NULL;
    PRINT '✅ Added updated_at column';
END

-- Add suspension_reason column
IF COL_LENGTH('dbo.companies', 'suspension_reason') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD suspension_reason NVARCHAR(500) NULL;
    PRINT '✅ Added suspension_reason column';
END

-- Add admin_notes column
IF COL_LENGTH('dbo.companies', 'admin_notes') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD admin_notes NVARCHAR(MAX) NULL;
    PRINT '✅ Added admin_notes column';
END

-- Verify all columns exist
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'companies'
ORDER BY ORDINAL_POSITION;