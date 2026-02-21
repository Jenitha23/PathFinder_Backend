using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace PATHFINDER_BACKEND.Data
{
    public class Db
    {
        private readonly string _connStr;

        public Db(IConfiguration config)
        {
            _connStr = config.GetConnectionString("Default")
                      ?? throw new Exception("Missing ConnectionStrings:Default");
        }

        public MySqlConnection CreateConnection() => new MySqlConnection(_connStr);
    }
}