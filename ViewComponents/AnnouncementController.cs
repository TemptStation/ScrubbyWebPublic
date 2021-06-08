using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Services;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.ViewComponents
{
    public class AnnouncementViewComponent : ViewComponent
    {
        private readonly IAnnouncementService _announcements;

        public AnnouncementViewComponent(IAnnouncementService announce)
        {
            _announcements = announce;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var announcements = await _announcements.GetAnnouncements();
            return View(announcements);
        }
    }
}