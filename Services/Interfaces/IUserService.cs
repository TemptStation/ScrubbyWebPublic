using System.Threading.Tasks;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface IUserService
    {
        public Task<ScrubbyUser> GetUser(string phpbbUsername);
        public Task<ScrubbyUser> CreateUser(ScrubbyUser user);
        public Task<ScrubbyUser> UpdateUser(ScrubbyUser user);
        public Task<bool> DeleteUser(ScrubbyUser user);
    }
}