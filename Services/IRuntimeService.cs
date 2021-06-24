using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services
{
    public interface IRuntimeService
    {
        Task<IEnumerable<ImprovedRuntime>> GetRuntimesForRound(int roundID);
    }
}