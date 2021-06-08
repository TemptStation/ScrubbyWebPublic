using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyCommon.Data.Events;

namespace ScrubbyWeb.Services
{
    public interface ISuicideService
    {
        Task<List<Suicide>> GetSuicidesForRound(int roundID);

        Task<List<Suicide>> GetSuicidesForCKey(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null);

        Task<List<Suicide>> GetSuicidesForCKey(CKey ckey, int startRound, int limit);
    }
}