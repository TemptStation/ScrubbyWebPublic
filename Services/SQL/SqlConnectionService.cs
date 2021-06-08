using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlConnectionService : IConnectionService
    {
        private readonly string _connectionString;

        public SqlConnectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("mn3");
        }
        
        public async Task<List<ServerRoundStatistic>> GetConnectionStatsForCKey(CKey ckey, DateTime startDate)
        {
            const string query = @"
                SELECT
                    s.display AS server,
                    r.starttime::date AS date,
                    COUNT(r.id),
                    ROUND((EXTRACT(EPOCH FROM SUM(c.disconnect_time - c.connect_time)) / (3600.0))::numeric, 2) AS hours
                FROM
                    round r
                    INNER JOIN connection c ON c.round = r.id
                    LEFT JOIN server s ON r.server = s.id
                WHERE
                    c.ckey = @ckey
                    AND c.connect_time >= @startDate
                GROUP BY
                    s.display,
                    r.starttime::date";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<ServerRoundStatistic>(query, new {ckey = ckey.Cleaned, startDate})).ToList();
        }
    }
}