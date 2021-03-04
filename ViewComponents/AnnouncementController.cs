using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.ViewComponents
{
    public class AnnouncementViewComponent : ViewComponent
    {
        private readonly AnnouncementService _announcements;

        public AnnouncementViewComponent(AnnouncementService announce)
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