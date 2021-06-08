using System.Collections.Generic;
using System.Threading.Tasks;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services
{
    public interface IAnnouncementService
    {
        Task<List<ScrubbyAnnouncement>> GetAnnouncements();
    }
}