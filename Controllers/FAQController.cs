using System.Threading.Tasks;
using Markdig;
using Markdig.SyntaxHighlighting;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ScrubbyWeb.Controllers
{
    public class FAQController : Controller
    {
        // GET: /<controller>/
        public async Task<IActionResult> Index()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSyntaxHighlighting()
                .UseBootstrap()
                .Build();

            var page = Markdown.ToHtml(await GetFAQMarkdown(), pipeline);

            return View("FAQ", new FAQViewModel {Raw = page});
        }

        [ResponseCache(Duration = 300)]
        private static async Task<string> GetFAQMarkdown()
        {
            return System.IO.File.ReadAllText(@"Views/FAQ/FAQ.md");
        }

        public async Task<IActionResult> SecurityPolicy()
        {
            return View("SecurityPolicy");
        }
    }
}