using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyCommon.Data.Events;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface ISuicideService
    {
        Task<List<Suicide>> GetSuicidesForCKey(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null);
    }
}