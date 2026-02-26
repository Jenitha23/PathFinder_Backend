using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace PATHFINDER_BACKEND.Data
{
    public class Db
    {
        private readonly string _connStr;

        public Db(IConfiguration config)
        {
            // Get connection string from env variable or appsettings.json
            _connStr = Environment.GetEnvironmentVariable("DefaultConnection")
                      ?? config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Missing ConnectionStrings:DefaultConnection or DB_CONNECTION_STRING");
        }

        // Create SQL Server connection
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connStr);
        }
    }
}
