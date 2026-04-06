using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.DTOs;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Handles all database operations related to Company entities.
    /// Enhanced with approval workflow, filtering, pagination, and audit logging.
    /// </summary>
    public class CompanyRepository
    {
        private readonly Db _db;
        public CompanyRepository(Db db) => _db = db;

        /// <summary>
        /// Fetch a company by ID.
        /// </summary>
        public async Task<Company?> GetByIdAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1) 
                    id, company_name, email, password_hash, status, created_at,
                    description, industry, website, location, phone, logo_url,
                    rejection_reason, approved_by, approved_at, updated_by, updated_at, admin_notes
                FROM companies
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return MapToCompany(reader);
        }

        /// <summary>
        /// Fetch a company by email.
        /// </summary>
        public async Task<Company?> GetByEmailAsync(string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1) 
                    id, company_name, email, password_hash, status, created_at,
                    description, industry, website, location, phone, logo_url,
                    rejection_reason, approved_by, approved_at, updated_by, updated_at, admin_notes
                FROM companies
                WHERE email = @email;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return MapToCompany(reader);
        }

        /// <summary>
        /// Get companies with filtering and pagination - FIXED VERSION
        /// </summary>
        public async Task<(List<CompanyListItemResponse> Companies, int TotalCount)> GetCompaniesFilteredAsync(
            CompanyListFilterRequest filter)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            // Status filter
            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "ALL")
            {
                conditions.Add("c.status = @status");
                parameters.Add(new SqlParameter("@status", filter.Status));
            }

            // Search term (company name or email)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                conditions.Add("(c.company_name LIKE @search OR c.email LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            // Date range filter
            if (filter.FromDate.HasValue)
            {
                conditions.Add("c.created_at >= @fromDate");
                parameters.Add(new SqlParameter("@fromDate", filter.FromDate.Value));
            }
            if (filter.ToDate.HasValue)
            {
                conditions.Add("c.created_at <= @toDate");
                parameters.Add(new SqlParameter("@toDate", filter.ToDate.Value.AddDays(1)));
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Sorting
            var orderBy = filter.SortBy?.ToLower() switch
            {
                "created_at_asc" => "ORDER BY c.created_at ASC",
                "company_name" => "ORDER BY c.company_name ASC",
                "company_name_desc" => "ORDER BY c.company_name DESC",
                "status" => "ORDER BY c.status ASC",
                "status_desc" => "ORDER BY c.status DESC",
                _ => "ORDER BY c.created_at DESC"
            };

            // Count query
            var countSql = $@"
                SELECT COUNT(DISTINCT c.id)
                FROM dbo.companies c
                {whereClause}";

            int totalCount;
            using (var countCmd = new SqlCommand(countSql, conn))
            {
                // Add parameters to count command - create new parameters, don't reuse
                foreach (var p in parameters)
                {
                    countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                }
                totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            if (totalCount == 0)
            {
                return (new List<CompanyListItemResponse>(), 0);
            }

            // Data query with stats
            var offset = (filter.Page - 1) * filter.PageSize;
            var dataSql = $@"
                SELECT 
                    c.id,
                    c.company_name,
                    c.email,
                    c.status,
                    c.rejection_reason,
                    c.created_at,
                    c.approved_at,
                    a.full_name AS approved_by_name,
                    COUNT(DISTINCT j.id) AS total_jobs,
                    COUNT(DISTINCT ap.id) AS total_applications
                FROM dbo.companies c
                LEFT JOIN dbo.admins a ON c.approved_by = a.id
                LEFT JOIN dbo.jobs j ON j.company_id = c.id AND (j.is_deleted IS NULL OR j.is_deleted = 0)
                LEFT JOIN dbo.applications ap ON ap.job_id = j.id
                {whereClause}
                GROUP BY 
                    c.id, c.company_name, c.email, c.status, 
                    c.rejection_reason, c.created_at, c.approved_at, a.full_name
                {orderBy}
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var companies = new List<CompanyListItemResponse>();
            using (var dataCmd = new SqlCommand(dataSql, conn))
            {
                // Add parameters to data command - create new parameters, don't reuse
                foreach (var p in parameters)
                {
                    dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                }
                dataCmd.Parameters.AddWithValue("@offset", offset);
                dataCmd.Parameters.AddWithValue("@pageSize", filter.PageSize);

                using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    companies.Add(new CompanyListItemResponse
                    {
                        Id = reader.GetInt32(0),
                        CompanyName = reader.GetString(1),
                        Email = reader.GetString(2),
                        Status = reader.GetString(3),
                        RejectionReason = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CreatedAt = reader.GetDateTime(5),
                        ApprovedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                        ApprovedByName = reader.IsDBNull(7) ? null : reader.GetString(7),
                        TotalJobsPosted = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                        TotalApplications = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                        CanBeApproved = reader.GetString(3) == "PENDING_APPROVAL"
                    });
                }
            }

            return (companies, totalCount);
        }

        /// <summary>
        /// Get pending companies count.
        /// </summary>
        public async Task<int> GetPendingCompaniesCountAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT COUNT(1) FROM dbo.companies WHERE status = 'PENDING_APPROVAL'";
            using var cmd = new SqlCommand(sql, conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Get company job count.
        /// </summary>
        public async Task<int> GetCompanyJobCountAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT COUNT(1) 
                FROM dbo.jobs 
                WHERE company_id = @companyId AND (is_deleted IS NULL OR is_deleted = 0)";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Get company for review with full details.
        /// </summary>
        public async Task<Company?> GetCompanyForReviewAsync(int companyId)
        {
            return await GetByIdAsync(companyId);
        }

        /// <summary>
        /// Enhanced status update with audit trail.
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateCompanyStatusWithAuditAsync(
            int companyId,
            string status,
            int adminId,
            string? rejectionReason = null,
            string? adminNotes = null)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Get current status first
            var currentStatus = await GetCompanyStatusSimpleAsync(companyId, conn);
            if (currentStatus == null)
            {
                return (false, "Company not found.");
            }

            if (currentStatus == status)
            {
                return (false, $"Company is already {status}.");
            }

            // Update company status
            const string updateSql = @"
                UPDATE dbo.companies
                SET 
                    status = @status,
                    rejection_reason = @rejectionReason,
                    admin_notes = @adminNotes,
                    approved_by = CASE 
                        WHEN @status IN ('APPROVED', 'REJECTED') THEN @adminId 
                        ELSE approved_by 
                    END,
                    approved_at = CASE 
                        WHEN @status IN ('APPROVED', 'REJECTED') AND approved_at IS NULL THEN SYSUTCDATETIME()
                        ELSE approved_at 
                    END,
                    updated_by = @adminId,
                    updated_at = SYSUTCDATETIME()
                WHERE id = @companyId";

            using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@status", status);
            updateCmd.Parameters.AddWithValue("@rejectionReason", (object?)rejectionReason ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@adminNotes", (object?)adminNotes ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@adminId", adminId);
            updateCmd.Parameters.AddWithValue("@companyId", companyId);

            var rowsAffected = await updateCmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return (false, "Failed to update company status.");
            }

            // Log audit trail
            await LogAuditSimpleAsync(adminId, companyId, currentStatus, status, rejectionReason);

            var message = status == "APPROVED"
                ? "Company approved successfully."
                : "Company rejected successfully.";

            return (true, message);
        }

        /// <summary>
        /// Bulk update company statuses.
        /// </summary>
        public async Task<(int SuccessCount, int FailCount, List<BulkOperationResult> Results)> BulkUpdateCompanyStatusAsync(
            List<int> companyIds,
            string status,
            int adminId,
            string? defaultRejectionReason = null,
            string? adminNotes = null)
        {
            var results = new List<BulkOperationResult>();
            int successCount = 0;
            int failCount = 0;

            foreach (var companyId in companyIds)
            {
                var (success, message) = await UpdateCompanyStatusWithAuditAsync(
                    companyId: companyId,
                    status: status,
                    adminId: adminId,
                    rejectionReason: status == "REJECTED" ? defaultRejectionReason : null,
                    adminNotes: adminNotes
                );

                results.Add(new BulkOperationResult
                {
                    CompanyId = companyId,
                    Success = success,
                    Message = message
                });

                if (success) successCount++;
                else failCount++;
            }

            return (successCount, failCount, results);
        }

        /// <summary>
        /// Get audit logs for a company.
        /// </summary>
        public async Task<List<AuditLog>> GetCompanyAuditLogsAsync(int companyId, int limit = 50)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (@limit) 
                    id, admin_id, company_id, action, old_value, new_value, details, created_at
                FROM dbo.admin_audit_logs
                WHERE company_id = @companyId
                ORDER BY created_at DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            var logs = new List<AuditLog>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32(0),
                    AdminId = reader.GetInt32(1),
                    CompanyId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Action = reader.GetString(3),
                    OldValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    NewValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Details = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAt = reader.GetDateTime(7)
                });
            }

            return logs;
        }

        /// <summary>
        /// Returns all companies (excluding password hash).
        /// </summary>
        public async Task<List<Company>> GetAllAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, company_name, email, status, created_at,
                       rejection_reason, approved_by, approved_at
                FROM companies
                ORDER BY id DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var companies = new List<Company>();
            while (await reader.ReadAsync())
            {
                companies.Add(new Company
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    RejectionReason = reader.IsDBNull(reader.GetOrdinal("rejection_reason")) ? null : reader.GetString(reader.GetOrdinal("rejection_reason")),
                    ApprovedBy = reader.IsDBNull(reader.GetOrdinal("approved_by")) ? null : reader.GetInt32(reader.GetOrdinal("approved_by")),
                    ApprovedAt = reader.IsDBNull(reader.GetOrdinal("approved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("approved_at"))
                });
            }

            return companies;
        }

        /// <summary>
        /// Creates a new company record and returns generated ID.
        /// </summary>
        public async Task<int> CreateAsync(Company c)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO companies (company_name, email, password_hash, status)
                VALUES (@name, @email, @hash, @status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", c.CompanyName);
            cmd.Parameters.AddWithValue("@email", c.Email);
            cmd.Parameters.AddWithValue("@hash", c.PasswordHash);
            cmd.Parameters.AddWithValue("@status", c.Status);

            var idObj = await cmd.ExecuteScalarAsync();
            return (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);
        }

        /// <summary>
        /// Updates company approval status (used by Admin).
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int companyId, string status)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET status = @status
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates company profile fields (name + email).
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int companyId, string companyName, string email)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET company_name = @name, email = @email
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", companyName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Updates company password hash.
        /// </summary>
        public async Task<bool> UpdatePasswordHashAsync(int companyId, string passwordHash)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE companies
                SET password_hash = @hash
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", passwordHash);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Deletes a company by ID.
        /// </summary>
        public async Task<bool> DeleteByIdAsync(int companyId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                DELETE FROM companies
                WHERE id = @id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ========== PRIVATE HELPER METHODS ==========

        /// <summary>
        /// Get company status (simple version without transaction).
        /// </summary>
        private async Task<string?> GetCompanyStatusSimpleAsync(int companyId, SqlConnection conn)
        {
            const string sql = "SELECT status FROM dbo.companies WHERE id = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        /// <summary>
        /// Log audit trail (simple version without transaction).
        /// </summary>
        private async Task LogAuditSimpleAsync(int adminId, int companyId, string oldStatus, string newStatus, string? rejectionReason)
        {
            try
            {
                await using var conn = _db.CreateConnection();
                await conn.OpenAsync();

                const string sql = @"
                    INSERT INTO dbo.admin_audit_logs (admin_id, company_id, action, old_value, new_value, details, created_at)
                    VALUES (@adminId, @companyId, @action, @oldValue, @newValue, @details, SYSUTCDATETIME())";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@adminId", adminId);
                cmd.Parameters.AddWithValue("@companyId", companyId);
                cmd.Parameters.AddWithValue("@action", $"STATUS_CHANGED_TO_{newStatus}");
                cmd.Parameters.AddWithValue("@oldValue", oldStatus);
                cmd.Parameters.AddWithValue("@newValue", newStatus);
                cmd.Parameters.AddWithValue("@details", rejectionReason != null ? $"Rejection reason: {rejectionReason}" : (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main operation
                Console.WriteLine($"Failed to log audit: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps SQL data reader to Company object.
        /// </summary>
        private Company MapToCompany(SqlDataReader reader)
        {
            return new Company
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString(reader.GetOrdinal("industry")),
                Website = reader.IsDBNull(reader.GetOrdinal("website")) ? null : reader.GetString(reader.GetOrdinal("website")),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                LogoUrl = reader.IsDBNull(reader.GetOrdinal("logo_url")) ? null : reader.GetString(reader.GetOrdinal("logo_url")),
                
                RejectionReason = reader.IsDBNull(reader.GetOrdinal("rejection_reason")) ? null : reader.GetString(reader.GetOrdinal("rejection_reason")),
                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("approved_by")) ? null : reader.GetInt32(reader.GetOrdinal("approved_by")),
                ApprovedAt = reader.IsDBNull(reader.GetOrdinal("approved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("approved_at")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                AdminNotes = reader.IsDBNull(reader.GetOrdinal("admin_notes")) ? null : reader.GetString(reader.GetOrdinal("admin_notes"))
            };
        }
    }
}