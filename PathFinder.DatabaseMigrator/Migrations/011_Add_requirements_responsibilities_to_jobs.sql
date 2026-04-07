BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'jobs' AND COLUMN_NAME = 'requirements'
    )
    BEGIN
        ALTER TABLE dbo.jobs ADD requirements NVARCHAR(MAX) NULL;
        PRINT 'Added requirements column';
    END;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'jobs' AND COLUMN_NAME = 'responsibilities'
    )
    BEGIN
        ALTER TABLE dbo.jobs ADD responsibilities NVARCHAR(MAX) NULL;
        PRINT 'Added responsibilities column';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;