using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyWeb.Models.CommonRounds;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface IRoundService
    {
        Task<ScrubbyRound> GetRound(int id);
        Task<int> GetNext(int id, bool forward = true, List<string> ckey = null);
        Task<List<CommonRoundModel>> GetCommonRounds(IEnumerable<string> ckeys, CommonRoundsOptions options = null);
    }
}