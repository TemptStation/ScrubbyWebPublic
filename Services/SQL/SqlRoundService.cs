using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.CommonRounds;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlRoundService : SqlServiceBase, IRoundService
    {
        public SqlRoundService(IConfiguration configuration) : base(configuration)
        {
        }
        
        public async Task<Round> GetRound(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<CommonRoundModel>> GetCommonRounds(IEnumerable<string> ckeys, CommonRoundsOptions options = null)
        {
            const string query = @"
                WITH common_rounds AS (
                    SELECT
                        c.round
                    FROM
                        connection c
                    WHERE
                        c.ckey = ANY(@ckeys)
                        AND (@startingRound IS NULL OR (
                            CASE WHEN @gte THEN c.round >= @startingRound ELSE c.round <= @startingRound END))
                    GROUP BY
                        c.round
                    HAVING
                        COUNT(DISTINCT c.ckey) = @ckeyCount
                    LIMIT @limit
                )
                SELECT
                    r.id AS round,
                    r.starttime AS started,
                    r.endtime AS ended
                FROM
                    common_rounds cr
                    INNER JOIN round r ON r.id = cr.round
                ORDER BY
                    r.id ASC";
            await using var conn = GetConnection();
            return (await conn.QueryAsync<CommonRoundModel>(query, new
            {
                ckeys, 
                ckeyCount = ckeys.Count(), 
                startingRound = options?.StartingRound,
                gte = options?.GTERound,
                limit = options?.Limit
            })).ToList();
        }
    }
}