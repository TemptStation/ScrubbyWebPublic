using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ScrubbyWeb.Controllers
{
    public class NewsController : Controller
    {
        private readonly IMongoCollection<NewsItemModel> _news;

        public NewsController(MongoAccess mongo)
        {
            _news = mongo.DB.GetCollection<NewsItemModel>("scrubby_news");
        }

        // GET: /<controller>/
        public async Task<IActionResult> Index()
        {
            return View("NewsIndex", new NewsViewModel {NewsItems = await GetNews()});
        }

        public async Task<List<NewsItemModel>> GetNews(int limit = 10)
        {
            return await _news.Find(x => true).SortByDescending(x => x.Date).Limit(limit).ToListAsync();
        }
    }
}