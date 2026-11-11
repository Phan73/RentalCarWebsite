using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectPartB.Models;
using ProjectPartB.Models.ViewModels;
using ProjectPartB.Services;

namespace ProjectPartB.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFleetService _fleetService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFleetService fleetService, ILogger<HomeController> logger)
        {
            _fleetService = fleetService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var featured = await _fleetService.GetFeaturedVehiclesAsync(6);
            var locations = await _fleetService.GetAllLocationsAsync();
            var categories = await _fleetService.GetActiveCategoriesAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedVehicles = featured,
                Locations = locations,
                Categories = categories,
                DefaultPickupDate = DateTime.Now.Date.AddDays(1).AddHours(10),
                DefaultReturnDate = DateTime.Now.Date.AddDays(5).AddHours(10)
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var locations = await _fleetService.GetAllLocationsAsync();
            return View(locations);
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
