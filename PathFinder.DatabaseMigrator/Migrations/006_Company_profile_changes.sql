-- Add new columns to existing companies table without dropping data

-- Add description column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'description')
BEGIN
    ALTER TABLE companies ADD description NVARCHAR(MAX) NULL;
    PRINT 'Added description column';
END

-- Add industry column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'industry')
BEGIN
    ALTER TABLE companies ADD industry NVARCHAR(150) NULL;
    PRINT 'Added industry column';
END

-- Add website column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'website')
BEGIN
    ALTER TABLE companies ADD website NVARCHAR(300) NULL;
    PRINT 'Added website column';
END

-- Add location column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'location')
BEGIN
    ALTER TABLE companies ADD location NVARCHAR(200) NULL;
    PRINT 'Added location column';
END

-- Add phone column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'phone')
BEGIN
    ALTER TABLE companies ADD phone NVARCHAR(50) NULL;
    PRINT 'Added phone column';
END

-- Add logo_url column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'companies' AND COLUMN_NAME = 'logo_url')
BEGIN
    ALTER TABLE companies ADD logo_url NVARCHAR(500) NULL;
    PRINT 'Added logo_url column';
END

-- Create indexes for better performance (if they don't exist)
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_companies_status' AND object_id = OBJECT_ID('companies'))
BEGIN
    CREATE INDEX IX_companies_status ON companies(status);
    PRINT 'Created index on status';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_companies_industry' AND object_id = OBJECT_ID('companies'))
BEGIN
    CREATE INDEX IX_companies_industry ON companies(industry);
    PRINT 'Created index on industry';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_companies_location' AND object_id = OBJECT_ID('companies'))
BEGIN
    CREATE INDEX IX_companies_location ON companies(location);
    PRINT 'Created index on location';
END

PRINT 'Companies table update completed successfully!';