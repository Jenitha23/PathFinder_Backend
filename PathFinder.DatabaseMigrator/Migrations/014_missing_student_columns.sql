-- =============================================
-- Migration: Add missing columns to students table
-- Date: 2026-04-09
-- Description: Adds columns for soft delete, status tracking, and audit fields
-- =============================================

-- Check if columns exist before adding (prevents errors if re-running)
-- Add status column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'status')
BEGIN
    ALTER TABLE students ADD status NVARCHAR(50) NOT NULL DEFAULT 'ACTIVE'
    PRINT 'Added column: status'
END
ELSE
    PRINT 'Column status already exists'

-- Add is_deleted column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'is_deleted')
BEGIN
    ALTER TABLE students ADD is_deleted BIT NOT NULL DEFAULT 0
    PRINT 'Added column: is_deleted'
END
ELSE
    PRINT 'Column is_deleted already exists'

-- Add deleted_at column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'deleted_at')
BEGIN
    ALTER TABLE students ADD deleted_at DATETIME2 NULL
    PRINT 'Added column: deleted_at'
END
ELSE
    PRINT 'Column deleted_at already exists'

-- Add updated_at column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'updated_at')
BEGIN
    ALTER TABLE students ADD updated_at DATETIME2 NULL
    PRINT 'Added column: updated_at'
END
ELSE
    PRINT 'Column updated_at already exists'

-- Add suspension_reason column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'suspension_reason')
BEGIN
    ALTER TABLE students ADD suspension_reason NVARCHAR(500) NULL
    PRINT 'Added column: suspension_reason'
END
ELSE
    PRINT 'Column suspension_reason already exists'

-- =============================================
-- Optional: Create an index for better performance
-- =============================================

-- Index for filtering by is_deleted (useful for soft delete queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_is_deleted' AND object_id = OBJECT_ID('students'))
BEGIN
    CREATE INDEX IX_students_is_deleted ON students(is_deleted)
    PRINT 'Created index: IX_students_is_deleted'
END

-- Index for filtering by status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_status' AND object_id = OBJECT_ID('students'))
BEGIN
    CREATE INDEX IX_students_status ON students(status)
    PRINT 'Created index: IX_students_status'
END

-- =============================================
-- Verify the changes
-- =============================================

-- Display all columns in the students table
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'students'
ORDER BY ORDINAL_POSITION

PRINT 'Migration completed successfully!'