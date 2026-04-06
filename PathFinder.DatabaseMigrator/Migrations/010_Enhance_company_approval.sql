-- 010_Enhance_company_approval.sql
-- Adds approval workflow columns and audit logging

BEGIN TRY
    BEGIN TRANSACTION;

    -- Add rejection reason column
    IF COL_LENGTH('dbo.companies', 'rejection_reason') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD rejection_reason NVARCHAR(500) NULL;
        PRINT 'Added rejection_reason column';
    END

    -- Add approved_by column (admin who approved/rejected)
    IF COL_LENGTH('dbo.companies', 'approved_by') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD approved_by INT NULL;
        PRINT 'Added approved_by column';
    END

    -- Add approved_at timestamp
    IF COL_LENGTH('dbo.companies', 'approved_at') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD approved_at DATETIME2 NULL;
        PRINT 'Added approved_at column';
    END

    -- Add updated_by column
    IF COL_LENGTH('dbo.companies', 'updated_by') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD updated_by INT NULL;
        PRINT 'Added updated_by column';
    END

    -- Add updated_at column
    IF COL_LENGTH('dbo.companies', 'updated_at') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD updated_at DATETIME2 NULL;
        PRINT 'Added updated_at column';
    END

    -- Add admin_notes column for internal notes
    IF COL_LENGTH('dbo.companies', 'admin_notes') IS NULL
    BEGIN
        ALTER TABLE dbo.companies ADD admin_notes NVARCHAR(MAX) NULL;
        PRINT 'Added admin_notes column';
    END

    -- Add foreign key for approved_by
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_companies_admins_approved')
    BEGIN
        ALTER TABLE dbo.companies 
        ADD CONSTRAINT FK_companies_admins_approved 
        FOREIGN KEY (approved_by) REFERENCES dbo.admins(id);
        PRINT 'Added FK for approved_by';
    END

    -- Create indexes for better performance
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_companies_status_created')
    BEGIN
        CREATE INDEX IX_companies_status_created ON dbo.companies(status, created_at);
        PRINT 'Created index on status and created_at';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_companies_approved_at')
    BEGIN
        CREATE INDEX IX_companies_approved_at ON dbo.companies(approved_at);
        PRINT 'Created index on approved_at';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_companies_approved_by')
    BEGIN
        CREATE INDEX IX_companies_approved_by ON dbo.companies(approved_by);
        PRINT 'Created index on approved_by';
    END

    -- Create admin audit logs table
    IF OBJECT_ID('dbo.admin_audit_logs', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.admin_audit_logs (
            id INT IDENTITY(1,1) PRIMARY KEY,
            admin_id INT NOT NULL,
            company_id INT NULL,
            action NVARCHAR(100) NOT NULL,
            old_value NVARCHAR(500) NULL,
            new_value NVARCHAR(500) NULL,
            details NVARCHAR(MAX) NULL,
            created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT FK_audit_logs_admins 
                FOREIGN KEY (admin_id) REFERENCES dbo.admins(id),
            
            CONSTRAINT FK_audit_logs_companies 
                FOREIGN KEY (company_id) REFERENCES dbo.companies(id) ON DELETE SET NULL
        );
        PRINT 'Created admin_audit_logs table';
        
        -- Create indexes for audit logs
        CREATE INDEX IX_audit_logs_admin_id ON dbo.admin_audit_logs(admin_id);
        CREATE INDEX IX_audit_logs_company_id ON dbo.admin_audit_logs(company_id);
        CREATE INDEX IX_audit_logs_created_at ON dbo.admin_audit_logs(created_at);
        CREATE INDEX IX_audit_logs_action ON dbo.admin_audit_logs(action);
        PRINT 'Created audit logs indexes';
    END

    COMMIT TRANSACTION;
    PRINT 'Company approval enhancements completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH