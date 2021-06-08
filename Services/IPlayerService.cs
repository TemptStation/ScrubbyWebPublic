using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services
{
    public interface IPlayerService
    {
        Task<List<PlayerNameStatistic>> SearchForCKey(Regex regex);
        Task<List<PlayerNameStatistic>> SearchForICName(Regex regex);
        Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, int startingRound, int limit);

        Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null);
    }
}