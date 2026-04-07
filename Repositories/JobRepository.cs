using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;

namespace PATHFINDER_BACKEND.Repositories
{
    public class JobRepository
    {
        private readonly Db _db;

        public JobRepository(Db db)
        {
            _db = db;
        }

        public async Task EnsureTableAndIndexesAsync()
        {
            var sql = @"
IF OBJECT_ID('dbo.jobs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.jobs (
        id INT IDENTITY(1,1) PRIMARY KEY,
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(MAX) NOT NULL,
        requirements NVARCHAR(MAX) NULL,
        responsibilities NVARCHAR(MAX) NULL,
        company_id INT NOT NULL,
        location NVARCHAR(150) NOT NULL,
        salary NVARCHAR(100) NULL,
        type NVARCHAR(50) NOT NULL,
        deadline DATE NOT NULL,
        category NVARCHAR(100) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_jobs_companies
            FOREIGN KEY (company_id)
            REFERENCES dbo.companies(id)
            ON DELETE CASCADE
    );
END;

-- Add requirements column if not exists (for existing tables)
IF COL_LENGTH('dbo.jobs', 'requirements') IS NULL
    ALTER TABLE dbo.jobs ADD requirements NVARCHAR(MAX) NULL;

-- Add responsibilities column if not exists (for existing tables)
IF COL_LENGTH('dbo.jobs', 'responsibilities') IS NULL
    ALTER TABLE dbo.jobs ADD responsibilities NVARCHAR(MAX) NULL;

-- Ensure all other columns exist
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

-- Create indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_jobs_title' AND object_id = OBJECT_ID('dbo.jobs'))
    CREATE INDEX idx_jobs_title ON dbo.jobs(title);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_jobs_location' AND object_id = OBJECT_ID('dbo.jobs'))
    CREATE INDEX idx_jobs_location ON dbo.jobs(location);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_jobs_type' AND object_id = OBJECT_ID('dbo.jobs'))
    CREATE INDEX idx_jobs_type ON dbo.jobs(type);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_jobs_category' AND object_id = OBJECT_ID('dbo.jobs'))
    CREATE INDEX idx_jobs_category ON dbo.jobs(category);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_jobs_company_id' AND object_id = OBJECT_ID('dbo.jobs'))
    CREATE INDEX idx_jobs_company_id ON dbo.jobs(company_id);


";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<PagedJobsResponse> GetJobsAsync(
            int page,
            int pageSize,
            string? keyword,
            string? title,
            string? company,
            string? location,
            string? type,
            string? category)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            // 1. Keyword search (Title, Description, Company) - Multi-token
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var tokens = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var keywordClauses = new List<string>();
                for (int i = 0; i < tokens.Length; i++)
                {
                    var pName = $"@kw{i}";
                    keywordClauses.Add($"(j.title LIKE '%' + {pName} + '%' OR j.description LIKE '%' + {pName} + '%' OR c.company_name LIKE '%' + {pName} + '%')");
                    parameters.Add(new SqlParameter(pName, tokens[i]));
                }
                whereClauses.Add($"({string.Join(" AND ", keywordClauses)})");
            }

            // 2. Title Filter
            if (!string.IsNullOrWhiteSpace(title))
            {
                whereClauses.Add("j.title LIKE '%' + @title + '%'");
                parameters.Add(new SqlParameter("@title", title.Trim()));
            }

            // 3. Company Filter
            if (!string.IsNullOrWhiteSpace(company))
            {
                whereClauses.Add("c.company_name LIKE '%' + @company + '%'");
                parameters.Add(new SqlParameter("@company", company.Trim()));
            }

            // 4. Location Filter - Multi-token for flexibility
            if (!string.IsNullOrWhiteSpace(location))
            {
                var locTokens = location.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var locClauses = new List<string>();
                for (int i = 0; i < locTokens.Length; i++)
                {
                    var pName = $"@loc{i}";
                    locClauses.Add($"j.location LIKE '%' + {pName} + '%'");
                    parameters.Add(new SqlParameter(pName, locTokens[i]));
                }
                whereClauses.Add($"({string.Join(" AND ", locClauses)})");
            }

            // 5. Type Filter
            if (!string.IsNullOrWhiteSpace(type))
            {
                whereClauses.Add("j.type LIKE '%' + @type + '%'");
                parameters.Add(new SqlParameter("@type", type.Trim()));
            }

            // 6. Category Filter - Fuzzy Multi-token
            if (!string.IsNullOrWhiteSpace(category))
            {
                var searchCat = category.ToLower();
                if (searchCat.Contains("quality") || searchCat.Contains("assurance") || searchCat.Contains("qa"))
                {
                    whereClauses.Add("(j.category LIKE '%QA%' OR j.category LIKE '%Quality%' OR j.category LIKE '%Assurance%' OR j.title LIKE '%QA%')");
                }
                else
                {
                    var catTokens = category.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var catClauses = new List<string>();
                    for (int i = 0; i < catTokens.Length; i++)
                    {
                        var pName = $"@cat{i}";
                        catClauses.Add($"(j.category LIKE '%' + {pName} + '%' OR j.title LIKE '%' + {pName} + '%')");
                        parameters.Add(new SqlParameter(pName, catTokens[i]));
                    }
                    whereClauses.Add($"({string.Join(" AND ", catClauses)})");
                }
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            var countSql = $@"
SELECT COUNT(*)
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
{whereSql};
";

            var dataSql = $@"
SELECT
    j.id,
    j.title,
    c.company_name,
    j.location,
    j.type,
    j.category,
    j.salary,
    j.deadline
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
{whereSql}
ORDER BY j.created_at DESC, j.id DESC
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            int totalItems;
            using (var countCmd = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                totalItems = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var items = new List<JobListItemResponse>();
            using (var dataCmd = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

                using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new JobListItemResponse
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        CompanyName = reader.GetString(2),
                        Location = reader.GetString(3),
                        Type = reader.GetString(4),
                        Category = reader.GetString(5),
                        Salary = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Deadline = reader.GetDateTime(7)
                    });
                }
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return new PagedJobsResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<JobDetailsResponse?> GetJobByIdAsync(int id)
        {
            var sql = @"
SELECT
    j.id,
    j.title,
    j.description,
    j.requirements,           
    j.responsibilities,
    j.company_id,
    c.company_name,
    j.location,
    j.salary,
    j.type,
    j.category,
    j.deadline,
    j.created_at
FROM dbo.jobs j
INNER JOIN dbo.companies c ON j.company_id = c.id
WHERE j.id = @id;
";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new JobDetailsResponse
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Requirements = reader.IsDBNull(3) ? null : reader.GetString(3),
                Responsibilities = reader.IsDBNull(4) ? null : reader.GetString(4),
                CompanyId = reader.GetInt32(5),
                CompanyName = reader.GetString(6),
                Location = reader.GetString(7),
                Salary = reader.IsDBNull(8) ? null : reader.GetString(8),
                Type = reader.GetString(9),
                Category = reader.GetString(10),
                Deadline = reader.GetDateTime(11),
                CreatedAt = reader.GetDateTime(12)
            };
        }

        private static void AddCommonParameters(
            SqlCommand cmd,
            string? keyword,
            string? title,
            string? company,
            string? location,
            string? type,
            string? category)
        {
            cmd.Parameters.AddWithValue("@keyword", (object?)Normalize(keyword) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", (object?)Normalize(title) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@company", (object?)Normalize(company) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@location", (object?)Normalize(location) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@type", (object?)Normalize(type) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@category", (object?)Normalize(category) ?? DBNull.Value);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}