using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface ICKeyService
    {
        Task<List<NameCountRecord>> GetNamesForCKeyAsync(CKey ckey);
        Task<List<ServerStatistic>> GetServerCountForCKeyAsync(CKey ckey);
        Task<string> GetByondKeyAsync(CKey ckey);
        Task<SqlCKey> GetKeyDetailsAsync(string ckey, bool requestIfNotFound = false);
    }
}