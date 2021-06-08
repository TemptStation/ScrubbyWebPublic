using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlCKeyService : ICKeyService
    {
        private readonly string _connectionString;

        public SqlCKeyService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("mn3");
        }
        
        public async Task<List<NameCountRecord>> GetNamesForCKeyAsync(CKey ckey)
        {
            const string query = @"
                SELECT
                    rp.name,
                    COUNT(*)
                FROM
                    round_player rp
                WHERE
                    rp.ckey = @ckey
                GROUP BY
                    rp.name
                ORDER BY
                    count desc";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<NameCountRecord>(query, new { ckey = ckey.Cleaned })).ToList();
        }

        public async Task<List<ServerStatistic>> GetServerCountForCKeyAsync(CKey ckey)
        {
            const string query = @"
                WITH rounds AS (
                    SELECT DISTINCT
                        c.round,
                        c.ckey
                    FROM
                        connection c
                    WHERE
                        c.ckey = @ckey
                )
                SELECT
                    s.display AS server,
                    COUNT(*) FILTER (WHERE rp.id IS NOT NULL) AS played,
                    COUNT(*) FILTER (WHERE rp.id IS NULL) AS connected,
                    COALESCE(EXTRACT(EPOCH FROM SUM((SELECT SUM(c.disconnect_time - c.connect_time) FROM connection c WHERE c.round = r_init.round AND c.ckey = r_init.ckey)) FILTER (WHERE rp.id IS NOT NULL)) * 1000, 0) AS playedmillisec,
                    COALESCE(EXTRACT(EPOCH FROM SUM((SELECT SUM(c.disconnect_time - c.connect_time) FROM connection c WHERE c.round = r_init.round AND c.ckey = r_init.ckey)) FILTER (WHERE rp.id IS NULL)) * 1000, 0) AS connectedmillisec 
                FROM
                    rounds r_init
                    INNER JOIN round r ON r.id = r_init.round
                    LEFT JOIN server s ON s.id = r.server
                    LEFT JOIN round_player rp ON rp.round = r_init.round AND rp.ckey = r_init.ckey
                GROUP BY
                    s.display";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<ServerStatistic>(query, new { ckey = ckey.Cleaned })).ToList();
        }

        public async Task<string> GetByondKeyAsync(CKey ckey)
        {
            const string query = @"SELECT ck.byond_key FROM ckey ck WHERE ck.ckey = @ckey";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<string>(query, new { ckey = ckey.Cleaned })).FirstOrDefault();
        }
    }
}