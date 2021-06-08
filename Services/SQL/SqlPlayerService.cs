using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlPlayerService : IPlayerService
    {
        private readonly string _connectionString;

        public SqlPlayerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("mn3");
        }
        
        public async Task<List<PlayerNameStatistic>> SearchForCKey(Regex regex)
        {
            const string query = @"
                SELECT
                    COALESCE(ck.byond_key, ck.ckey) AS CKey,
                    COUNT(*)
                FROM
                    ckey ck
                    LEFT JOIN connection c ON c.ckey = ck.ckey
                WHERE
                    ck.ckey ~* @regex
                GROUP BY
                    COALESCE(ck.byond_key, ck.ckey)
                ORDER BY
                    count DESC";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<PlayerNameStatistic>(query, new {regex = regex.ToString()})).ToList();
        }

        public async Task<List<PlayerNameStatistic>> SearchForICName(Regex regex)
        {
            const string query = @"
                SELECT
                    rp.name AS icname,
                    COALESCE(ck.byond_key, rp.ckey) AS ckey,
                    COUNT(*)
                FROM
                    round_player rp
                    LEFT JOIN ckey ck ON ck.ckey = rp.ckey
                WHERE
                    rp.name ~* @regex
                GROUP BY
                    rp.name,
                    COALESCE(ck.byond_key, rp.ckey)
                ORDER BY
                    count DESC";
            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<PlayerNameStatistic>(query, new {regex = regex.ToString()})).ToList();
        }

        public async Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, int startingRound, int limit)
        {
            const string query = @"
                WITH rounds AS (
                    SELECT DISTINCT c.round, c.ckey FROM connection c WHERE c.ckey = @ckey AND c.round < @startingRound ORDER BY c.round DESC LIMIT @limit
                )
                SELECT
                    r.id AS roundid,
                    rp.job,
                    r.starttime AS timestamp,
                    (SELECT SUM(c.disconnect_time - c.connect_time) FROM connection c WHERE c.ckey = r_init.ckey AND c.round = r.id) AS connectedtime,
                    CASE WHEN rp.ckey IS NOT NULL THEN EXISTS (
                        SELECT 1 
                        FROM 
                            round_file rf 
                            INNER JOIN log_message m ON m.parent_file = rf.id
                        WHERE
                            rf.round = r.id
                            AND rf.name = 'manifest.txt'
                            AND m.message ~* ('^' || rp.ckey || '.*roundstart$')
                        LIMIT 1
                    ) ELSE FALSE END AS roundstartplayer,
                    rp.job IS NOT NULL AS playedinround,
                    rp.role IS NOT NULL AS antagonist,
                    EXISTS (
                        SELECT 1
                        FROM
                            suicide s
                            INNER JOIN log_message m ON s.evidence = m.id
                        WHERE
                            s.round = r.id
                            AND s.ckey = r_init.ckey
                            AND m.timestamp < r.starttime + INTERVAL '10 minutes'
                        LIMIT 1
                    ) AS roundstartsuicide,
                    rp.name,
                    s.display AS server
                FROM
                    rounds r_init
                    INNER JOIN round r ON r_init.round = r.id 
                    LEFT JOIN server s ON s.id = r.server
                    LEFT JOIN round_player rp ON rp.round = r.id AND rp.ckey = r_init.ckey";

            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<RoundReceipt>(query, new { ckey = ckey.Cleaned, startingRound, limit })).ToList();
        }

        public async Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, DateTime? startDate = null, DateTime? endDate = null)
        {
            const string query = @"
                WITH rounds AS (
                    SELECT DISTINCT c.round, c.ckey 
                    FROM connection c 
                    WHERE 
                        c.ckey = @ckey AND (
                            (@startDate IS NULL AND @endDate IS NULL) 
                            OR (@startDate IS NULL AND c.connect_time < @endDate)
                            OR (@endDate IS NULL AND c.connect_time > @startDate)
                            OR (c.connect_time BETWEEN @startDate AND @endDate)
                        ) 
                    ORDER BY c.round DESC
                )
                SELECT
                    r.id AS roundid,
                    rp.job,
                    r.starttime AS timestamp,
                    (SELECT SUM(c.disconnect_time - c.connect_time) FROM connection c WHERE c.ckey = r_init.ckey AND c.round = r.id) AS connectedtime,
                    CASE WHEN rp.ckey IS NOT NULL THEN EXISTS (
                        SELECT 1 
                        FROM 
                            round_file rf 
                            INNER JOIN log_message m ON m.parent_file = rf.id
                        WHERE
                            rf.round = r.id
                            AND rf.name = 'manifest.txt'
                            AND m.message ~* ('^' || rp.ckey || '.*roundstart$')
                        LIMIT 1
                    ) ELSE FALSE END AS roundstartplayer,
                    rp.job IS NOT NULL AS playedinround,
                    rp.role IS NOT NULL AS antagonist,
                    EXISTS (
                        SELECT 1
                        FROM
                            suicide s
                            INNER JOIN log_message m ON s.evidence = m.id
                        WHERE
                            s.round = r.id
                            AND s.ckey = r_init.ckey
                            AND m.timestamp < r.starttime + INTERVAL '10 minutes'
                        LIMIT 1
                    ) AS roundstartsuicide,
                    rp.name,
                    s.display AS server
                FROM
                    rounds r_init
                    INNER JOIN round r ON r_init.round = r.id 
                    LEFT JOIN server s ON s.id = r.server
                    LEFT JOIN round_player rp ON rp.round = r.id AND rp.ckey = r_init.ckey";

            await using var conn = new NpgsqlConnection(_connectionString);
            return (await conn.QueryAsync<RoundReceipt>(query, new { ckey = ckey.Cleaned, startDate, endDate })).ToList();
        }
    }
}