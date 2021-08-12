using System.Threading.Tasks;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface INewscasterService
    {
        public Task<NewsCasterModel> GetRound(int round);
    }
}