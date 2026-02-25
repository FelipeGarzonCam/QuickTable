using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Models;
using System.Diagnostics;


namespace QuickTableProyect.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public IActionResult KeepAlive()
        {
            if (HttpContext.Session.IsAvailable)
            {
                // Tocar la sesión para renovar el timeout
                HttpContext.Session.SetString("KeepAlive", DateTime.Now.ToString("o"));

                var rol = HttpContext.Session.GetString("Rol");
                return Ok(new { success = true, timestamp = DateTime.Now, rol = rol });
            }
            return Ok(new { success = false });
        }

    }
}
