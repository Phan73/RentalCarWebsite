using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;
using ProjectPartB.Models.ViewModels;
using ProjectPartB.Services;

namespace ProjectPartB.Controllers
{
    public class CarDescriptionsController : Controller
    {
        private readonly FS23_Group4_ProjectContext _context;
        private readonly IFleetService _fleetService;
        private readonly IPricingService _pricingService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public CarDescriptionsController(
            FS23_Group4_ProjectContext context,
            IFleetService fleetService,
            IPricingService pricingService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _fleetService = fleetService;
            _pricingService = pricingService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: CarDescriptions
        public async Task<IActionResult> Index(
            string? searchString,
            int? categoryId,
            string? transmission,
            string? fuelType,
            int? minSeats,
            decimal? minPrice,
            decimal? maxPrice,
            DateTime? pickupDateTime,
            DateTime? returnDateTime,
            int? pickupLocationId,
            int? returnLocationId,
            string? sortOrder,
            int? page)
        {
            var filter = new VehicleSearchFilter
            {
                SearchTerm = searchString,
                CategoryId = categoryId,
                Transmission = transmission,
                FuelType = fuelType,
                MinSeats = minSeats,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PickupDateTime = pickupDateTime,
                ReturnDateTime = returnDateTime,
                PickupLocationId = pickupLocationId,
                ReturnLocationId = returnLocationId,
                SortBy = sortOrder,
                Page = page ?? 1,
                PageSize = 9
            };

            var searchResult = await _fleetService.SearchVehiclesAsync(filter);
            var categories = await _fleetService.GetActiveCategoriesAsync();
            var locations = await _fleetService.GetAllLocationsAsync();

            var viewModel = new VehicleListViewModel
            {
                SearchResult = searchResult,
                Categories = categories,
                Locations = locations,
                Filter = filter
            };

            return View(viewModel);
        }

        // Non-blocking AJAX auto-suggest endpoint
        [HttpGet]
        public async Task<IActionResult> IndexAJAX(string? searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString) || searchString.Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            var term = searchString.Trim().ToLower();
            var suggestions = await _context.CarDescriptions
                .Where(c => (c.Brand != null && c.Brand.ToLower().Contains(term))
                         || (c.Model != null && c.Model.ToLower().Contains(term)))
                .Take(6)
                .Select(c => new
                {
                    id = c.CarDescriptionId,
                    brand = c.Brand,
                    model = c.Model,
                    fullName = c.Brand + " " + c.Model,
                    rate = c.RatePerDay
                })
                .ToListAsync();

            return Json(suggestions);
        }

        // GET: CarDescriptions/Details/5
        public async Task<IActionResult> Details(int? id, DateTime? pickupDateTime = null, DateTime? returnDateTime = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _fleetService.GetVehicleDetailsAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            var pDate = pickupDateTime ?? DateTime.Now.Date.AddDays(1).AddHours(10);
            var rDate = returnDateTime ?? DateTime.Now.Date.AddDays(5).AddHours(10);
            var pricing = _pricingService.CalculateBookingPrice(vehicle.RatePerDay, pDate, rDate);
            var locations = await _fleetService.GetAllLocationsAsync();

            var viewModel = new VehicleDetailViewModel
            {
                Vehicle = vehicle,
                Locations = locations,
                DefaultPickupDate = pDate,
                DefaultReturnDate = rDate,
                EstimatedPricing = pricing
            };

            return View(viewModel);
        }

        // GET: CarDescriptions/Create
        [Authorize(Roles = "Administrator,Manager,Editor")]
        public async Task<IActionResult> Create()
        {
            var categories = await _fleetService.GetActiveCategoriesAsync();
            ViewData["CarTypeId"] = new SelectList(categories, "Id", "Name");
            return View();
        }

        // POST: CarDescriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager,Editor")]
        public async Task<IActionResult> Create(CarDescription carDescription, IFormFile? imageFile)
        {
            var categories = await _fleetService.GetActiveCategoriesAsync();
            var selectedCategory = categories.FirstOrDefault(c => c.Id == carDescription.CarTypeId);
            if (selectedCategory != null)
            {
                carDescription.CarTypeName = selectedCategory.Name;
            }

            // Secure File Upload Validation
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("Image", "Vehicle image must be under 5 MB.");
                }

                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Image", "Only .jpg, .jpeg, .png, and .webp images are permitted.");
                }

                if (ModelState.IsValid)
                {
                    var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    carDescription.Image = uniqueFileName;
                }
            }

            if (ModelState.IsValid)
            {
                _context.CarDescriptions.Add(carDescription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CarTypeId"] = new SelectList(categories, "Id", "Name", carDescription.CarTypeId);
            return View(carDescription);
        }

        // GET: CarDescriptions/Edit/5
        [Authorize(Roles = "Administrator,Manager,Editor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var carDescription = await _context.CarDescriptions.FindAsync(id.Value);
            if (carDescription == null) return NotFound();

            var categories = await _fleetService.GetActiveCategoriesAsync();
            ViewData["CarTypeId"] = new SelectList(categories, "Id", "Name", carDescription.CarTypeId);
            return View(carDescription);
        }

        // POST: CarDescriptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager,Editor")]
        public async Task<IActionResult> Edit(int id, CarDescription carDescription, IFormFile? imageFile)
        {
            if (id != carDescription.CarDescriptionId) return NotFound();

            var categories = await _fleetService.GetActiveCategoriesAsync();
            var selectedCategory = categories.FirstOrDefault(c => c.Id == carDescription.CarTypeId);
            if (selectedCategory != null)
            {
                carDescription.CarTypeName = selectedCategory.Name;
            }

            // Secure File Upload Validation
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("Image", "Vehicle image must be under 5 MB.");
                }

                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Image", "Only .jpg, .jpeg, .png, and .webp images are permitted.");
                }

                if (ModelState.IsValid)
                {
                    var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    carDescription.Image = uniqueFileName;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carDescription);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.CarDescriptions.AnyAsync(e => e.CarDescriptionId == carDescription.CarDescriptionId))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewData["CarTypeId"] = new SelectList(categories, "Id", "Name", carDescription.CarTypeId);
            return View(carDescription);
        }

        // GET: CarDescriptions/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var carDescription = await _context.CarDescriptions
                .Include(c => c.CarType)
                .FirstOrDefaultAsync(m => m.CarDescriptionId == id);

            if (carDescription == null) return NotFound();

            return View(carDescription);
        }

        // POST: CarDescriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carDescription = await _context.CarDescriptions.FindAsync(id);
            if (carDescription != null)
            {
                // Soft archive or check for active bookings
                var hasActiveBookings = await _context.Bookings
                    .AnyAsync(b => b.CarDescriptionId == id && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Completed);

                if (hasActiveBookings)
                {
                    TempData["ErrorMessage"] = "Cannot delete vehicle with active or upcoming reservations. Marked as unavailable instead.";
                    carDescription.Available = false;
                }
                else
                {
                    _context.CarDescriptions.Remove(carDescription);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
