IF OBJECT_ID('dbo.applications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.applications (
        id INT IDENTITY(1,1) PRIMARY KEY,
        student_id INT NOT NULL,
        job_id INT NOT NULL,
        status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        applied_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        cover_letter NVARCHAR(MAX) NULL,

        CONSTRAINT FK_applications_students
            FOREIGN KEY (student_id) REFERENCES dbo.students(id)
            ON DELETE CASCADE,

        CONSTRAINT FK_applications_jobs
            FOREIGN KEY (job_id) REFERENCES dbo.jobs(id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_applications_student_job
            UNIQUE (student_id, job_id)
    );
END;

IF COL_LENGTH('dbo.applications', 'student_id') IS NULL
    ALTER TABLE dbo.applications ADD student_id INT NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.applications', 'job_id') IS NULL
    ALTER TABLE dbo.applications ADD job_id INT NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.applications', 'status') IS NULL
    ALTER TABLE dbo.applications ADD status NVARCHAR(50) NOT NULL DEFAULT 'Pending';

IF COL_LENGTH('dbo.applications', 'applied_date') IS NULL
    ALTER TABLE dbo.applications ADD applied_date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();

IF COL_LENGTH('dbo.applications', 'cover_letter') IS NULL
    ALTER TABLE dbo.applications ADD cover_letter NVARCHAR(MAX) NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE name = 'UQ_applications_student_job' AND type = 'UQ'
)
BEGIN
    ALTER TABLE dbo.applications
        ADD CONSTRAINT UQ_applications_student_job UNIQUE (student_id, job_id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_applications_student_id'
      AND object_id = OBJECT_ID('dbo.applications')
)
BEGIN
    CREATE INDEX idx_applications_student_id ON dbo.applications(student_id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_applications_job_id'
      AND object_id = OBJECT_ID('dbo.applications')
)
BEGIN
    CREATE INDEX idx_applications_job_id ON dbo.applications(job_id);
END;