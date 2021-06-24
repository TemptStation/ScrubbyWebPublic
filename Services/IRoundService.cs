using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Api;
using ScrubbyWeb.Models.CommonRounds;

namespace ScrubbyWeb.Services
{
    public interface IRoundService
    {
        Task<Round> GetRound(int id);
        Task<List<CommonRoundModel>> GetCommonRounds(IEnumerable<string> ckeys, CommonRoundsOptions options = null);
    }
}