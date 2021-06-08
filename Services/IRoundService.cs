using System.Threading.Tasks;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services
{
    public interface IRoundService
    {
        Task<Round> GetRound(int id);
    }
}