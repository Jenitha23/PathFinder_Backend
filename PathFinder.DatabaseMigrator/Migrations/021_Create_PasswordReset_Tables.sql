-- =============================================
-- Migration: Password Reset Functionality
-- Description: Tables for storing password reset tokens
-- Date: 2026-04-16
-- =============================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- Create password reset tokens table
    IF OBJECT_ID('dbo.password_reset_tokens', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.password_reset_tokens (
            id INT IDENTITY(1,1) PRIMARY KEY,
            email NVARCHAR(150) NOT NULL,
            token NVARCHAR(255) NOT NULL,
            user_type NVARCHAR(20) NOT NULL, -- 'STUDENT' or 'COMPANY'
            used BIT NOT NULL DEFAULT 0,
            expires_at DATETIME2 NOT NULL,
            created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT UQ_password_reset_tokens_token UNIQUE (token)
        );
        
        -- Create indexes for performance
        CREATE INDEX IX_password_reset_tokens_email ON dbo.password_reset_tokens(email);
        CREATE INDEX IX_password_reset_tokens_token ON dbo.password_reset_tokens(token);
        CREATE INDEX IX_password_reset_tokens_expires_at ON dbo.password_reset_tokens(expires_at);
        
        PRINT '✅ Created password_reset_tokens table';
    END

    COMMIT TRANSACTION;
    PRINT '✅ Password reset migration completed successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH