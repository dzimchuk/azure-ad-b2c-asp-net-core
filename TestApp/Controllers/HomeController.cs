using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using TestApp.Models;
using TestApp.Proxy;

namespace TestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly TestServiceProxy testService;

        public HomeController(TestServiceProxy testService)
        {
            this.testService = testService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Test()
        {
            ViewData["Message"] = $"Hello {User.Identity.Name}!";
            var forecast = await testService.GetWeatherForecastAsync();

            return View(forecast);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
