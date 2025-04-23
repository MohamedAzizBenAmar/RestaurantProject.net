using Microsoft.AspNetCore.Mvc;
using restaurant.Data;
using restaurant.Models;
using System.Diagnostics;
using System.Threading.Tasks;

namespace restaurant.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Repository<Product> _products;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _products = new Repository<Product>(context);
        }

        public async Task<IActionResult> Index()
        {
            var products = await _products.GetAllAsync();
            return View(products);
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
    }
}