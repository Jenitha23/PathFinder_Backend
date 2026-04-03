IF OBJECT_ID('dbo.jobs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.jobs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(MAX) NOT NULL,
        company_id INT NOT NULL,
        location NVARCHAR(150) NOT NULL,
        salary NVARCHAR(100) NULL,
        type NVARCHAR(50) NOT NULL,
        deadline DATE NOT NULL,
        category NVARCHAR(100) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        is_deleted BIT NOT NULL DEFAULT 0,
        updated_at DATETIME2 NULL,

        CONSTRAINT FK_jobs_companies
            FOREIGN KEY (company_id)
            REFERENCES dbo.companies(id)
            ON DELETE CASCADE
    );
END;

IF COL_LENGTH('dbo.jobs', 'title') IS NULL
    ALTER TABLE dbo.jobs ADD title NVARCHAR(200) NOT NULL DEFAULT '';

IF COL_LENGTH('dbo.jobs', 'description') IS NULL
    ALTER TABLE dbo.jobs ADD description NVARCHAR(MAX) NOT NULL DEFAULT '';

IF COL_LENGTH('dbo.jobs', 'company_id') IS NULL
    ALTER TABLE dbo.jobs ADD company_id INT NOT NULL DEFAULT 1;

IF COL_LENGTH('dbo.jobs', 'location') IS NULL
    ALTER TABLE dbo.jobs ADD location NVARCHAR(150) NOT NULL DEFAULT '';

IF COL_LENGTH('dbo.jobs', 'salary') IS NULL
    ALTER TABLE dbo.jobs ADD salary NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.jobs', 'type') IS NULL
    ALTER TABLE dbo.jobs ADD type NVARCHAR(50) NOT NULL DEFAULT '';

IF COL_LENGTH('dbo.jobs', 'deadline') IS NULL
    ALTER TABLE dbo.jobs ADD deadline DATE NOT NULL DEFAULT GETDATE();

IF COL_LENGTH('dbo.jobs', 'category') IS NULL
    ALTER TABLE dbo.jobs ADD category NVARCHAR(100) NOT NULL DEFAULT '';

IF COL_LENGTH('dbo.jobs', 'created_at') IS NULL
    ALTER TABLE dbo.jobs ADD created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF COL_LENGTH('dbo.jobs', 'is_deleted') IS NULL
    ALTER TABLE dbo.jobs ADD is_deleted BIT NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.jobs', 'updated_at') IS NULL
    ALTER TABLE dbo.jobs ADD updated_at DATETIME2 NULL;

UPDATE dbo.jobs
SET updated_at = created_at
WHERE updated_at IS NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_jobs_title' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX idx_jobs_title ON dbo.jobs(title);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_jobs_location' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX idx_jobs_location ON dbo.jobs(location);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_jobs_type' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX idx_jobs_type ON dbo.jobs(type);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_jobs_category' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX idx_jobs_category ON dbo.jobs(category);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_jobs_company_id' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX idx_jobs_company_id ON dbo.jobs(company_id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_jobs_is_deleted' AND object_id = OBJECT_ID('dbo.jobs')
)
BEGIN
    CREATE INDEX IX_jobs_is_deleted ON dbo.jobs(is_deleted);
END;