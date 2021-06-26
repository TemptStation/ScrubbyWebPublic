using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ScrubbyWeb.Services.SQL
{
    public abstract class SqlServiceBase
    {
        private readonly string _connectionString;
        
        public SqlServiceBase(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("mn3");
        }

        protected NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);
    }
}