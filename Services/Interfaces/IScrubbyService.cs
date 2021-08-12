using System.Threading.Tasks;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface IScrubbyService
    {
        public Task<BasicStatsModel> GetBasicStats();
    }
}