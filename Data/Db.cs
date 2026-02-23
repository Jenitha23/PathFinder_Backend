// using Microsoft.Extensions.Configuration;
// //using MySqlConnector;
// using Microsoft.Data.SqlClient;

// namespace PATHFINDER_BACKEND.Data
// {
//     public class Db
//     {
//         private readonly string _connStr;

//         public Db(IConfiguration config)
//         {
//             // Try to get from environment variable first (Docker), then from appsettings
//             _connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
//                       ?? config.GetConnectionString("DefaultConnection")
//                       ?? throw new Exception("Missing ConnectionStrings:DefaultConnection or DB_CONNECTION_STRING environment variable");
//         }

//         public MySqlConnection CreateConnection() => new MySqlConnection(_connStr);
//     }
// }

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
            _connStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                      ?? config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Missing ConnectionStrings:DefaultConnection or DB_CONNECTION_STRING");
        }

        // Create SQL Server (Azure SQL) connection
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connStr);
        }
    }
}