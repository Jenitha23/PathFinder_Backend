-- Ensure applications table status column supports all required values
-- The column already exists from migration 008, just need to ensure CHECK constraint or update default

-- Add CHECK constraint to enforce valid status values (optional but recommended)
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints 
    WHERE name = 'CK_applications_status' AND parent_object_id = OBJECT_ID('dbo.applications')
)
BEGIN
    ALTER TABLE dbo.applications
    ADD CONSTRAINT CK_applications_status 
    CHECK (status IN ('Pending', 'Shortlisted', 'Rejected', 'Accepted'));
    
    PRINT 'Added CHECK constraint for application status values';
END

-- Update existing records to ensure they have valid status
UPDATE dbo.applications 
SET status = 'Pending' 
WHERE status NOT IN ('Pending', 'Shortlisted', 'Rejected', 'Accepted');

PRINT 'Application status values updated successfully';