using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Models;
using ProjectPartB.Models.ViewModels;
using ProjectPartB.Services;

namespace ProjectPartB.Controllers
{
    public class LivePriceCalculationRequest
    {
        public int CarId { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime ReturnDateTime { get; set; }
        public bool IncludeGps { get; set; }
        public bool IncludeChildSeat { get; set; }
        public bool IncludeAdditionalDriver { get; set; }
        public bool IncludeZeroExcess { get; set; }
    }

    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IFleetService _fleetService;
        private readonly IPricingService _pricingService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly UserManager<WebUser> _userManager;

        public BookingController(
            IBookingService bookingService,
            IFleetService fleetService,
            IPricingService pricingService,
            IEmailNotificationService emailNotificationService,
            UserManager<WebUser> userManager)
        {
            _bookingService = bookingService;
            _fleetService = fleetService;
            _pricingService = pricingService;
            _emailNotificationService = emailNotificationService;
            _userManager = userManager;
        }

        // GET: Booking/Reserve
        public async Task<IActionResult> Reserve(int carId, DateTime? pickupDateTime, DateTime? returnDateTime, int? pickupLocationId, int? returnLocationId)
        {
            var vehicle = await _fleetService.GetVehicleDetailsAsync(carId);
            if (vehicle == null || vehicle.Available == false)
            {
                TempData["ErrorMessage"] = "The selected vehicle is currently unavailable for online booking.";
                return RedirectToAction("Index", "CarDescriptions");
            }

            var locations = await _fleetService.GetAllLocationsAsync();
            var pDate = pickupDateTime ?? DateTime.Now.Date.AddDays(1).AddHours(10);
            var rDate = returnDateTime ?? DateTime.Now.Date.AddDays(5).AddHours(10);

            var viewModel = new BookingFormViewModel
            {
                CarDescriptionId = carId,
                Vehicle = vehicle,
                Locations = locations,
                PickupLocationId = pickupLocationId ?? locations.FirstOrDefault()?.LocationId ?? 1,
                ReturnLocationId = returnLocationId ?? locations.FirstOrDefault()?.LocationId ?? 1,
                PickupDateTime = pDate,
                ReturnDateTime = rDate,
                PaymentMethod = "Pay at Pickup"
            };

            // Pre-fill authenticated customer details
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    viewModel.CustomerName = user.FullName ?? "";
                    viewModel.CustomerEmail = user.Email ?? "";
                    viewModel.CustomerPhone = user.Phone ?? "";
                    viewModel.DriverLicenseNumber = user.DrivingLicense ?? "";
                }
            }

            viewModel.CalculatedPricing = _pricingService.CalculateBookingPrice(vehicle.RatePerDay, pDate, rDate);

            return View(viewModel);
        }

        // AJAX live pricing calculation endpoint
        [HttpPost]
        public async Task<IActionResult> CalculatePrice([FromBody] LivePriceCalculationRequest request)
        {
            if (request.ReturnDateTime <= request.PickupDateTime)
            {
                return BadRequest(new { message = "Return date and time must be after pickup date and time." });
            }

            var vehicle = await _fleetService.GetVehicleDetailsAsync(request.CarId);
            if (vehicle == null)
            {
                return NotFound();
            }

            var extras = new List<PricingExtraRequest>();
            if (request.IncludeGps)
            {
                extras.Add(new PricingExtraRequest { Name = "GPS Navigation", DailyPrice = 10.00m, Quantity = 1 });
            }
            if (request.IncludeChildSeat)
            {
                extras.Add(new PricingExtraRequest { Name = "Child Safety Seat", DailyPrice = 12.00m, Quantity = 1 });
            }
            if (request.IncludeAdditionalDriver)
            {
                extras.Add(new PricingExtraRequest { Name = "Additional Authorized Driver", DailyPrice = 8.00m, Quantity = 1 });
            }
            if (request.IncludeZeroExcess)
            {
                extras.Add(new PricingExtraRequest { Name = "Zero-Excess Comprehensive Cover", DailyPrice = 25.00m, Quantity = 1 });
            }

            var pricing = _pricingService.CalculateBookingPrice(
                vehicle.RatePerDay,
                request.PickupDateTime,
                request.ReturnDateTime,
                extras,
                request.IncludeZeroExcess);

            return Json(pricing);
        }

        // POST: Booking/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(BookingFormViewModel model)
        {
            var vehicle = await _fleetService.GetVehicleDetailsAsync(model.CarDescriptionId);
            if (vehicle == null)
            {
                return NotFound();
            }

            var locations = await _fleetService.GetAllLocationsAsync();
            model.Vehicle = vehicle;
            model.Locations = locations;

            // Basic validation
            if (string.IsNullOrWhiteSpace(model.CustomerName))
            {
                ModelState.AddModelError("CustomerName", "Customer full name is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CustomerEmail))
            {
                ModelState.AddModelError("CustomerEmail", "Valid email address is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CustomerPhone))
            {
                ModelState.AddModelError("CustomerPhone", "Phone contact number is required.");
            }
            if (string.IsNullOrWhiteSpace(model.DriverLicenseNumber))
            {
                ModelState.AddModelError("DriverLicenseNumber", "Driver's licence number is required for insurance compliance.");
            }
            if (model.ReturnDateTime <= model.PickupDateTime)
            {
                ModelState.AddModelError("ReturnDateTime", "Return date and time must be after the pickup date.");
            }

            if (!ModelState.IsValid)
            {
                model.CalculatedPricing = _pricingService.CalculateBookingPrice(vehicle.RatePerDay, model.PickupDateTime, model.ReturnDateTime);
                return View("Reserve", model);
            }

            // Determine User ID (authenticated user or guest)
            string userId = "guest";
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                    // Update user profile if driving licence or phone was empty
                    if (string.IsNullOrEmpty(user.DrivingLicense))
                    {
                        user.DrivingLicense = model.DriverLicenseNumber.Trim();
                        user.Phone = model.CustomerPhone.Trim();
                        await _userManager.UpdateAsync(user);
                    }
                }
            }

            // Assemble Selected Extras
            var selectedExtras = new List<PricingExtraRequest>();
            if (model.IncludeGps)
            {
                selectedExtras.Add(new PricingExtraRequest { Name = "GPS Navigation", DailyPrice = 10.00m, Quantity = 1 });
            }
            if (model.IncludeChildSeat)
            {
                selectedExtras.Add(new PricingExtraRequest { Name = "Child Safety Seat", DailyPrice = 12.00m, Quantity = 1 });
            }
            if (model.IncludeAdditionalDriver)
            {
                selectedExtras.Add(new PricingExtraRequest { Name = "Additional Authorized Driver", DailyPrice = 8.00m, Quantity = 1 });
            }
            if (model.IncludeZeroExcess)
            {
                selectedExtras.Add(new PricingExtraRequest { Name = "Zero-Excess Comprehensive Cover", DailyPrice = 25.00m, Quantity = 1 });
            }

            var request = new BookingCreateRequest
            {
                UserId = userId,
                CustomerName = model.CustomerName,
                CustomerEmail = model.CustomerEmail,
                CustomerPhone = model.CustomerPhone,
                DriverLicenseNumber = model.DriverLicenseNumber,
                CarDescriptionId = model.CarDescriptionId,
                PickupLocationId = model.PickupLocationId,
                ReturnLocationId = model.ReturnLocationId,
                PickupDateTime = model.PickupDateTime,
                ReturnDateTime = model.ReturnDateTime,
                CustomerNotes = model.CustomerNotes,
                PaymentMethod = model.PaymentMethod,
                SelectedExtras = selectedExtras
            };

            var result = await _bookingService.CreateBookingAsync(request);
            if (!result.Success || result.Booking == null)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Could not create booking.");
                model.CalculatedPricing = _pricingService.CalculateBookingPrice(vehicle.RatePerDay, model.PickupDateTime, model.ReturnDateTime, selectedExtras);
                return View("Reserve", model);
            }

            // Send confirmation email
            await _emailNotificationService.SendBookingConfirmationAsync(result.Booking);

            return RedirectToAction(nameof(Confirmation), new { reference = result.Booking.BookingReference });
        }

        // GET: Booking/Confirmation
        public async Task<IActionResult> Confirmation(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = await _bookingService.GetBookingByReferenceAsync(reference);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Booking/MyBookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var bookings = await _bookingService.GetUserBookingsAsync(user.Id);
            return View(bookings);
        }

        // POST: Booking/Cancel
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            bool isAdmin = User.IsInRole("Administrator") || User.IsInRole("Manager");
            var result = await _bookingService.CancelBookingAsync(id, user.Id, reason, isAdmin);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Reservation {result.Booking?.BookingReference} has been successfully cancelled.";
                if (result.Booking != null)
                {
                    await _emailNotificationService.SendBookingCancellationAsync(result.Booking);
                }
            }

            if (isAdmin && Request.Headers["Referer"].ToString().Contains("/Admin"))
            {
                return RedirectToAction("Bookings", "Admin");
            }

            return RedirectToAction(nameof(MyBookings));
        }
    }
}
