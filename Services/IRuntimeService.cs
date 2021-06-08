using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services
{
    public interface IRuntimeService
    {
        Task<IEnumerable<Runtime>> GetRuntimesForRound(int roundID);

        Task<IEnumerable<Runtime>> GetRuntimesForCommit(string commitID, DateTime startDate,
            DateTime endDate);

        Task<IEnumerable<Runtime>> GetRuntimesForPR(int pr, DateTime startDate, DateTime endDate);
    }
}