using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;
using ProjectPartB.Models.ViewModels;
using ProjectPartB.Services;

namespace ProjectPartB.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Staff")]
    public class AdminController : Controller
    {
        private readonly FS23_Group4_ProjectContext _context;
        private readonly IFleetService _fleetService;
        private readonly IBookingService _bookingService;

        public AdminController(
            FS23_Group4_ProjectContext context,
            IFleetService fleetService,
            IBookingService bookingService)
        {
            _context = context;
            _fleetService = fleetService;
            _bookingService = bookingService;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            var stats = await _fleetService.GetFleetStatisticsAsync();
            var recentBookings = await _bookingService.GetAllBookingsAsync();
            var vehicles = await _context.CarDescriptions
                .Include(c => c.CarType)
                .OrderBy(c => c.Brand)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Stats = stats,
                RecentBookings = recentBookings.Take(8).ToList(),
                Vehicles = vehicles
            };

            return View(viewModel);
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Bookings(BookingStatus? status, string? search)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(status, search);
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = search;
            return View(bookings);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, BookingStatus newStatus)
        {
            var result = await _bookingService.UpdateBookingStatusAsync(id, newStatus);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Booking {result.Booking?.BookingReference} status updated to {newStatus}.";
            }

            return RedirectToAction(nameof(Bookings));
        }

        // GET: Admin/Fleet
        public async Task<IActionResult> Fleet()
        {
            var vehicles = await _context.CarDescriptions
                .Include(c => c.CarType)
                .Include(c => c.MaintenanceRecords)
                .OrderBy(c => c.Brand)
                .ThenBy(c => c.Model)
                .ToListAsync();

            return View(vehicles);
        }

        // POST: Admin/ToggleAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var vehicle = await _context.CarDescriptions.FindAsync(id);
            if (vehicle != null)
            {
                vehicle.Available = !(vehicle.Available ?? true);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{vehicle.Brand} {vehicle.Model} availability updated to {(vehicle.Available.Value ? "Available" : "Unavailable")}.";
            }

            return RedirectToAction(nameof(Fleet));
        }

        // POST: Admin/AddMaintenance
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> AddMaintenance(int carId, string description, decimal cost)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Maintenance description is required.";
                return RedirectToAction(nameof(Fleet));
            }

            var record = new MaintenanceRecord
            {
                CarDescriptionId = carId,
                MaintenanceDate = DateTime.UtcNow,
                Description = description.Trim(),
                Cost = Math.Max(0, cost),
                IsResolved = false
            };

            _context.MaintenanceRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Maintenance record logged successfully.";
            return RedirectToAction(nameof(Fleet));
        }
    }
}
