using Microsoft.AspNetCore.Mvc;

namespace PDXLite.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewBag.AppUrl = _configuration["AppSettings:AppUrl"] ?? Request.Scheme + "://" + Request.Host;
            ViewBag.AppName = _configuration["AppSettings:AppName"] ?? "PDXLite";
            ViewBag.Environment = _configuration["AppSettings:Environment"] ?? "Development";

            return View();
        }
    }
}
