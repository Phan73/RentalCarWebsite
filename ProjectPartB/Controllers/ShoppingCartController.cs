using Microsoft.AspNetCore.Mvc;

namespace ProjectPartB.Controllers
{
    // Modernized bridge: redirects legacy shopping cart actions to the robust booking engine
    public class ShoppingCartController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("MyBookings", "Booking");
        }

        public IActionResult AddToCart(int Id)
        {
            return RedirectToAction("Reserve", "Booking", new { carId = Id });
        }

        public IActionResult RemoveFromCart(int productId)
        {
            return RedirectToAction("Index", "CarDescriptions");
        }

        public IActionResult ClearCart()
        {
            return RedirectToAction("Index", "CarDescriptions");
        }
    }
}
