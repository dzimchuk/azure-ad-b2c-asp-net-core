using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> About()
        {
            ViewData["Message"] = $"Hello {User.Identity.Name}!";
            ViewData["Values"] = await testService.GetValuesAsync();

            return View();
        }
        
        public IActionResult Contact()
        {
            ViewData["Message"] = "Contact";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
