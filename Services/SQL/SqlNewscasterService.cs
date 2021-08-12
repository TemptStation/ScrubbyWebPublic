using System.Threading.Tasks;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.SQL
{
    public class SqlNewscasterService : INewscasterService
    {
        public async Task<NewsCasterModel> GetRound(int round)
        {
            throw new System.NotImplementedException();
        }
    }
}