using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Repository layer for Company Profile operations.
    /// Encapsulates all SQL queries for company profile data.
    /// Follows same pattern as StudentProfileRepository.
    /// </summary>
    public class CompanyProfileRepository
    {
        private readonly Db _db;

        public CompanyProfileRepository(Db db)
        {
            _db = db;
        }

        /// <summary>
        /// Ensures the companies table has all the required profile columns.
        /// Idempotent migration that can be called at startup.
        /// </summary>
        public async Task EnsureProfileColumnsAsync()
        {
            var sql = @"
-- Add description column if not exists
IF COL_LENGTH('dbo.companies', 'description') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD description NVARCHAR(MAX) NULL;
END

-- Add industry column if not exists
IF COL_LENGTH('dbo.companies', 'industry') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD industry NVARCHAR(150) NULL;
END

-- Add website column if not exists
IF COL_LENGTH('dbo.companies', 'website') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD website NVARCHAR(300) NULL;
END

-- Add location column if not exists
IF COL_LENGTH('dbo.companies', 'location') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD location NVARCHAR(200) NULL;
END

-- Add phone column if not exists
IF COL_LENGTH('dbo.companies', 'phone') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD phone NVARCHAR(50) NULL;
END

-- Add logo_url column if not exists
IF COL_LENGTH('dbo.companies', 'logo_url') IS NULL
BEGIN
    ALTER TABLE dbo.companies ADD logo_url NVARCHAR(500) NULL;
END";
            
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Fetches full company profile by ID including all profile fields.
        /// Returns null if company does not exist.
        /// </summary>
        public async Task<Company?> GetCompanyProfileAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1) 
                    id, 
                    company_name, 
                    email, 
                    password_hash, 
                    status, 
                    created_at,
                    description, 
                    industry, 
                    website, 
                    location, 
                    phone, 
                    logo_url
                FROM companies
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            // Get ordinals for better performance
            var idIdx = reader.GetOrdinal("id");
            var nameIdx = reader.GetOrdinal("company_name");
            var emailIdx = reader.GetOrdinal("email");
            var hashIdx = reader.GetOrdinal("password_hash");
            var statusIdx = reader.GetOrdinal("status");
            var createdIdx = reader.GetOrdinal("created_at");
            var descIdx = reader.GetOrdinal("description");
            var industryIdx = reader.GetOrdinal("industry");
            var websiteIdx = reader.GetOrdinal("website");
            var locationIdx = reader.GetOrdinal("location");
            var phoneIdx = reader.GetOrdinal("phone");
            var logoIdx = reader.GetOrdinal("logo_url");

            return new Company
            {
                Id = reader.GetInt32(idIdx),
                CompanyName = reader.GetString(nameIdx),
                Email = reader.GetString(emailIdx),
                PasswordHash = reader.GetString(hashIdx),
                Status = reader.GetString(statusIdx),
                CreatedAt = reader.GetDateTime(createdIdx),
                Description = reader.IsDBNull(descIdx) ? null : reader.GetString(descIdx),
                Industry = reader.IsDBNull(industryIdx) ? null : reader.GetString(industryIdx),
                Website = reader.IsDBNull(websiteIdx) ? null : reader.GetString(websiteIdx),
                Location = reader.IsDBNull(locationIdx) ? null : reader.GetString(locationIdx),
                Phone = reader.IsDBNull(phoneIdx) ? null : reader.GetString(phoneIdx),
                LogoUrl = reader.IsDBNull(logoIdx) ? null : reader.GetString(logoIdx)
            };
        }

        /// <summary>
        /// Updates company profile fields.
        /// Handles logo URL updates and removal based on flags.
        /// </summary>
        public async Task<bool> UpdateCompanyProfileAsync(
            int companyId,
            string companyName,
            string email,
            string? description,
            string? industry,
            string? website,
            string? location,
            string? phone,
            string? logoUrl,
            bool removeLogo)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql;
            
            // Build dynamic SQL based on logo operations
            if (removeLogo)
            {
                sql = @"
                    UPDATE companies
                    SET 
                        company_name = @name, 
                        email = @email,
                        description = @description,
                        industry = @industry,
                        website = @website,
                        location = @location,
                        phone = @phone,
                        logo_url = NULL
                    WHERE id = @id;";
            }
            else if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                sql = @"
                    UPDATE companies
                    SET 
                        company_name = @name, 
                        email = @email,
                        description = @description,
                        industry = @industry,
                        website = @website,
                        location = @location,
                        phone = @phone,
                        logo_url = @logoUrl
                    WHERE id = @id;";
            }
            else
            {
                sql = @"
                    UPDATE companies
                    SET 
                        company_name = @name, 
                        email = @email,
                        description = @description,
                        industry = @industry,
                        website = @website,
                        location = @location,
                        phone = @phone
                    WHERE id = @id;";
            }

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", companyName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@industry", (object?)industry ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@website", (object?)website ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@location", (object?)location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", companyId);
            
            if (!string.IsNullOrWhiteSpace(logoUrl) && !removeLogo)
            {
                cmd.Parameters.AddWithValue("@logoUrl", logoUrl);
            }

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates only the logo URL for a company.
        /// Useful for logo upload/removal operations.
        /// </summary>
        public async Task<bool> UpdateCompanyLogoAsync(int companyId, string? logoUrl)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET logo_url = @logoUrl
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@logoUrl", (object?)logoUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}