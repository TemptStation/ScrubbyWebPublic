using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services
{
    public interface IConnectionService
    {
        Task<List<ServerRoundStatistic>> GetConnectionStatsForCKey(CKey ckey, DateTime startDate);
        Task<List<ServerConnection>> GetConnectionsForRound(int round, IEnumerable<string> ckeys);
    }
}