using Microsoft.AspNetCore.Mvc;

namespace PDXLite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
