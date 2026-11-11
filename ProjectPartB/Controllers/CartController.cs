using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Models;

namespace ProjectPartB.Controllers
{
    // Hardened: strictly restricted to Administrators to prevent IDOR / customer PII leakage
    [Authorize(Roles = "Administrator")]
    public class CartController : Controller
    {
        private readonly FS23_Group4_ProjectContext _context;
        private readonly UserManager<WebUser> _userManager;

        public CartController(FS23_Group4_ProjectContext context, UserManager<WebUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userCarts = new List<UserCartViewModel>();

            foreach (var user in users)
            {
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Car)
                    .Where(ci => ci.AspNetUserId == user.Id)
                    .Select(ci => new CartDetailsViewModel
                    {
                        CarId = ci.Car.CarDescriptionId,
                        Quantity = ci.Quantity,
                        RentalDuration = ci.RentalDuration,
                        StartDate = ci.StartDate
                    }).ToListAsync();

                if (cartItems.Any())
                {
                    userCarts.Add(new UserCartViewModel
                    {
                        DriverLicense = user.DrivingLicense ?? "N/A",
                        FullName = user.FullName ?? user.UserName ?? "User",
                        CartItems = cartItems
                    });
                }
            }

            return View(userCarts);
        }
    }
}
