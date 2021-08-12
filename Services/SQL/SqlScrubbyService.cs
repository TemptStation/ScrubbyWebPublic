using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlScrubbyService : SqlServiceBase, IScrubbyService
    {
        public SqlScrubbyService(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<BasicStatsModel> GetBasicStats()
        {
            var toReturn = await GetStatsModelBase();
            toReturn.LatestRounds = (await GetLatest()).ToList();
            return toReturn;
        }

        private async Task<IEnumerable<LatestRound>> GetLatest()
        {
            await using var conn = GetConnection();
            return await conn.QueryAsync<LatestRound>(
                "SELECT r.server, r.starttime AS Started, r.round AS Id FROM latest_rounds r");
        }

        private async Task<BasicStatsModel> GetStatsModelBase()
        {
            const string query = @"
                SELECT COUNT(*) FROM round
                UNION ALL
                SELECT COUNT(*) FROM round_file";
            await using var conn = GetConnection();
            var counts = (await conn.QueryAsync<int>(query)).ToList();
            return new BasicStatsModel()
            {
                RoundCount = counts[0],
                FileCount = counts[1]
            };
        }
    }
}