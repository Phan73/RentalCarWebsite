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
            carDescription.CarTypeName = selectedCategory?.Name ?? "Standard";
            ModelState.Remove(nameof(CarDescription.CarTypeName));

            // Secure File Upload / Image Handling
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("imageFile", "Vehicle image must be under 5 MB.");
                }

                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("imageFile", "Only .jpg, .jpeg, .png, and .webp images are permitted.");
                }

                if (imageFile.Length <= MaxFileSize && AllowedExtensions.Contains(ext))
                {
                    carDescription.Image = await ProcessImageUploadAsync(imageFile);
                    ModelState.Remove(nameof(CarDescription.Image));
                }
            }
            else if (!string.IsNullOrWhiteSpace(carDescription.Image))
            {
                ModelState.Remove(nameof(CarDescription.Image));
            }
            else
            {
                // Fallback to high quality automotive image for category
                carDescription.Image = GetDefaultVehicleImage(carDescription.CarTypeName);
                ModelState.Remove(nameof(CarDescription.Image));
            }

            if (ModelState.IsValid)
            {
                _context.CarDescriptions.Add(carDescription);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Vehicle {carDescription.Brand} {carDescription.Model} was successfully added to the fleet.";
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
            carDescription.CarTypeName = selectedCategory?.Name ?? "Standard";
            ModelState.Remove(nameof(CarDescription.CarTypeName));

            var existingVehicle = await _context.CarDescriptions.AsNoTracking().FirstOrDefaultAsync(c => c.CarDescriptionId == id);
            if (existingVehicle == null) return NotFound();

            // Secure File Upload / Image Handling
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("imageFile", "Vehicle image must be under 5 MB.");
                }

                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("imageFile", "Only .jpg, .jpeg, .png, and .webp images are permitted.");
                }

                if (imageFile.Length <= MaxFileSize && AllowedExtensions.Contains(ext))
                {
                    carDescription.Image = await ProcessImageUploadAsync(imageFile);
                    ModelState.Remove(nameof(CarDescription.Image));
                }
            }
            else if (!string.IsNullOrWhiteSpace(carDescription.Image))
            {
                ModelState.Remove(nameof(CarDescription.Image));
            }
            else
            {
                // Preserve existing vehicle image if no new file or URL was provided
                carDescription.Image = existingVehicle.Image;
                ModelState.Remove(nameof(CarDescription.Image));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carDescription);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Vehicle {carDescription.Brand} {carDescription.Model} was successfully updated.";
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

        private async Task<string> ProcessImageUploadAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";

            // Try saving physical file to wwwroot/uploads if writable
            try
            {
                var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsDir = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                var filePath = Path.Combine(uploadsDir, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch
            {
                // Resilient fallback for serverless or read-only container volumes
            }

            // Convert to Base64 data URI for zero-dropout database storage across multi-instance serverless runtimes
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }

        private static string GetDefaultVehicleImage(string? categoryName)
        {
            return categoryName?.ToLowerInvariant() switch
            {
                "electric" => "https://images.unsplash.com/photo-1560958089-b8a1929cea89?auto=format&fit=crop&w=1200&q=80",
                "suv" => "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=1200&q=80",
                "luxury" => "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1200&q=80",
                "people mover" or "van" => "https://images.unsplash.com/photo-1559297434-fae8a1916a79?auto=format&fit=crop&w=1200&q=80",
                _ => "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80"
            };
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
