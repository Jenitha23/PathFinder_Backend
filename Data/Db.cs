using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace PATHFINDER_BACKEND.Data
{
    public class Db
    {
        private readonly string _connStr;

        public Db(IConfiguration config)
        {
            // Get connection string from env variable or appsettings.json
            _connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                      ?? config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Missing ConnectionStrings:DefaultConnection or DB_CONNECTION_STRING");
        }

        // Create MySQL connection
        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connStr);
        }
    }
}