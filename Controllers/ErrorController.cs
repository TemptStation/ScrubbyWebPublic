using Microsoft.AspNetCore.Mvc;

namespace ScrubbyWeb.Controllers
{
    [Route("[controller]")]
    public class ErrorController : Controller
    {
        [HttpGet("{id:int}")]
        public IActionResult Error(int id)
        {
            ViewBag.ErrorID = id;
            Response.StatusCode = id;
            return View();
        }
    }
}