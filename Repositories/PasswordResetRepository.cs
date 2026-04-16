using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.Models;

namespace PATHFINDER_BACKEND.Repositories
{
    public class PasswordResetRepository
    {
        private readonly Db _db;

        public PasswordResetRepository(Db db)
        {
            _db = db;
        }

        public async Task EnsureTableExistsAsync()
        {
            var sql = @"
                IF OBJECT_ID('dbo.password_reset_tokens', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.password_reset_tokens (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        email NVARCHAR(150) NOT NULL,
                        token NVARCHAR(255) NOT NULL,
                        user_type NVARCHAR(20) NOT NULL,
                        used BIT NOT NULL DEFAULT 0,
                        expires_at DATETIME2 NOT NULL,
                        created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        
                        CONSTRAINT UQ_password_reset_tokens_token UNIQUE (token)
                    );
                    
                    CREATE INDEX IX_password_reset_tokens_email ON dbo.password_reset_tokens(email);
                    CREATE INDEX IX_password_reset_tokens_token ON dbo.password_reset_tokens(token);
                    CREATE INDEX IX_password_reset_tokens_expires_at ON dbo.password_reset_tokens(expires_at);
                END";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveResetTokenAsync(PasswordResetToken token)
        {
            const string sql = @"
                INSERT INTO dbo.password_reset_tokens (email, token, user_type, used, expires_at, created_at)
                VALUES (@email, @token, @userType, @used, @expiresAt, @createdAt)";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", token.Email);
            cmd.Parameters.AddWithValue("@token", token.Token);
            cmd.Parameters.AddWithValue("@userType", token.UserType);
            cmd.Parameters.AddWithValue("@used", token.Used);
            cmd.Parameters.AddWithValue("@expiresAt", token.ExpiresAt);
            cmd.Parameters.AddWithValue("@createdAt", token.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<PasswordResetToken?> GetValidTokenAsync(string token)
        {
            const string sql = @"
                SELECT TOP 1 id, email, token, user_type, used, expires_at, created_at
                FROM dbo.password_reset_tokens
                WHERE token = @token 
                    AND used = 0 
                    AND expires_at > SYSUTCDATETIME()
                ORDER BY created_at DESC";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PasswordResetToken
                {
                    Id = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    Token = reader.GetString(2),
                    UserType = reader.GetString(3),
                    Used = reader.GetBoolean(4),
                    ExpiresAt = reader.GetDateTime(5),
                    CreatedAt = reader.GetDateTime(6)
                };
            }
            return null;
        }

        public async Task MarkTokenAsUsedAsync(int tokenId)
        {
            const string sql = @"
                UPDATE dbo.password_reset_tokens
                SET used = 1
                WHERE id = @id";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", tokenId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InvalidateAllTokensForEmailAsync(string email, string userType)
        {
            const string sql = @"
                UPDATE dbo.password_reset_tokens
                SET used = 1
                WHERE email = @email AND user_type = @userType AND used = 0";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@userType", userType);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}