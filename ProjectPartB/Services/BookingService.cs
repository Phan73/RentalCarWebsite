using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public class BookingService : IBookingService
    {
        private readonly FS23_Group4_ProjectContext _context;
        private readonly IPricingService _pricingService;

        public BookingService(FS23_Group4_ProjectContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<bool> IsVehicleAvailableAsync(int carDescriptionId, DateTime pickup, DateTime returnDate, int? excludeBookingId = null)
        {
            if (returnDate <= pickup)
            {
                return false;
            }

            var car = await _context.CarDescriptions.FindAsync(carDescriptionId);
            if (car == null || car.Available == false)
            {
                return false;
            }

            // Interval overlap check:
            // Existing booking overlaps if ExistingPickup < RequestedReturn AND ExistingReturn > RequestedPickup
            var pDate = pickup.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(pickup, DateTimeKind.Utc) : pickup.ToUniversalTime();
            var rDate = returnDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(returnDate, DateTimeKind.Utc) : returnDate.ToUniversalTime();

            var query = _context.Bookings
                .Where(b => b.CarDescriptionId == carDescriptionId
                         && b.Status != BookingStatus.Cancelled
                         && b.PickupDateTime < rDate
                         && b.ReturnDateTime > pDate);

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.BookingId != excludeBookingId.Value);
            }

            var hasOverlap = await query.AnyAsync();
            return !hasOverlap;
        }

        public async Task<BookingOperationResult> CreateBookingAsync(BookingCreateRequest request)
        {
            // 1. Validate dates
            if (request.ReturnDateTime <= request.PickupDateTime)
            {
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "Return date and time must be after the pickup date and time."
                };
            }

            if (request.PickupDateTime < DateTime.Now.AddMinutes(-30))
            {
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "Pickup date cannot be in the past."
                };
            }

            // 2. Validate customer information
            if (string.IsNullOrWhiteSpace(request.CustomerName) ||
                string.IsNullOrWhiteSpace(request.CustomerEmail) ||
                string.IsNullOrWhiteSpace(request.CustomerPhone) ||
                string.IsNullOrWhiteSpace(request.DriverLicenseNumber))
            {
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "Customer full name, email, phone number, and driver's licence are strictly required."
                };
            }

            // 3. Find vehicle
            var car = await _context.CarDescriptions
                .Include(c => c.CarType)
                .FirstOrDefaultAsync(c => c.CarDescriptionId == request.CarDescriptionId);

            if (car == null)
            {
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "Selected vehicle does not exist."
                };
            }

            // 4. Validate locations
            var pickupLoc = await _context.Locations.FindAsync(request.PickupLocationId);
            var returnLoc = await _context.Locations.FindAsync(request.ReturnLocationId);

            if (pickupLoc == null || returnLoc == null)
            {
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "Please select valid pickup and return branch locations."
                };
            }

            // 5. Calculate verified server pricing
            bool hasZeroExcess = request.SelectedExtras.Any(e => e.Name.Contains("Zero-Excess", StringComparison.OrdinalIgnoreCase));
            var pricing = _pricingService.CalculateBookingPrice(
                car.RatePerDay,
                request.PickupDateTime,
                request.ReturnDateTime,
                request.SelectedExtras,
                hasZeroExcess);

            // 6. Concurrency-safe reservation creation with Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var isAvailable = await IsVehicleAvailableAsync(request.CarDescriptionId, request.PickupDateTime, request.ReturnDateTime);
                if (!isAvailable)
                {
                    await transaction.RollbackAsync();
                    return new BookingOperationResult
                    {
                        Success = false,
                        ErrorMessage = "This vehicle was reserved by another customer for the selected dates. Please choose different dates or another premium vehicle."
                    };
                }

                var bookingRef = GenerateBookingReference();

                var booking = new Booking
                {
                    BookingReference = bookingRef,
                    UserId = request.UserId,
                    CustomerName = request.CustomerName.Trim(),
                    CustomerEmail = request.CustomerEmail.Trim(),
                    CustomerPhone = request.CustomerPhone.Trim(),
                    DriverLicenseNumber = request.DriverLicenseNumber.Trim().ToUpperInvariant(),
                    CarDescriptionId = request.CarDescriptionId,
                    PickupLocationId = request.PickupLocationId,
                    ReturnLocationId = request.ReturnLocationId,
                    PickupDateTime = request.PickupDateTime.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(request.PickupDateTime, DateTimeKind.Utc)
                        : request.PickupDateTime.ToUniversalTime(),
                    ReturnDateTime = request.ReturnDateTime.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(request.ReturnDateTime, DateTimeKind.Utc)
                        : request.ReturnDateTime.ToUniversalTime(),
                    RentalDays = pricing.RentalDays,
                    DailyRate = pricing.DailyRate,
                    DurationDiscountPercent = pricing.DurationDiscountPercent,
                    DiscountAmount = pricing.DiscountAmount,
                    ExtrasTotal = pricing.ExtrasTotal,
                    SubTotal = pricing.SubTotal,
                    TaxAmount = pricing.TaxAmount,
                    TotalAmount = pricing.TotalAmount,
                    SecurityDeposit = pricing.SecurityDeposit,
                    Status = BookingStatus.Confirmed,
                    CustomerNotes = request.CustomerNotes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                // Add Extras
                foreach (var extra in pricing.Extras)
                {
                    booking.BookingExtras.Add(new BookingExtra
                    {
                        Name = extra.Name,
                        DailyPrice = extra.DailyPrice,
                        TotalPrice = extra.TotalPrice,
                        Quantity = extra.Quantity
                    });
                }

                // Add Payment Record
                booking.Payment = new Payment
                {
                    TransactionReference = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                    PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Pay at Pickup" : request.PaymentMethod,
                    Amount = pricing.TotalAmount,
                    Currency = "NZD",
                    Status = request.PaymentMethod == "Credit Card (Simulated)" ? "Paid" : "Pending",
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new BookingOperationResult
                {
                    Success = true,
                    Booking = booking
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new BookingOperationResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while confirming your reservation: " + ex.Message
                };
            }
        }

        public async Task<Booking?> GetBookingByReferenceAsync(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;

            return await _context.Bookings
                .Include(b => b.CarDescription)
                    .ThenInclude(c => c!.CarType)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.BookingExtras)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingReference == reference.Trim());
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.CarDescription)
                    .ThenInclude(c => c!.CarType)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.BookingExtras)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        public async Task<List<Booking>> GetUserBookingsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<Booking>();

            return await _context.Bookings
                .Include(b => b.CarDescription)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.Payment)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.PickupDateTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetAllBookingsAsync(BookingStatus? status = null, string? search = null)
        {
            var query = _context.Bookings
                .Include(b => b.CarDescription)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.Payment)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(b => b.BookingReference.Contains(s)
                                      || b.CustomerName.Contains(s)
                                      || b.CustomerEmail.Contains(s)
                                      || b.DriverLicenseNumber.Contains(s));
            }

            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<BookingOperationResult> CancelBookingAsync(int bookingId, string userId, string reason, bool isAdmin = false)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return new BookingOperationResult { Success = false, ErrorMessage = "Booking not found." };
            }

            if (!isAdmin && booking.UserId != userId)
            {
                return new BookingOperationResult { Success = false, ErrorMessage = "Access denied. You can only cancel your own reservations." };
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return new BookingOperationResult { Success = false, ErrorMessage = "Reservation has already been cancelled." };
            }

            if (booking.Status == BookingStatus.Completed)
            {
                return new BookingOperationResult { Success = false, ErrorMessage = "Completed rentals cannot be cancelled." };
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by user" : reason.Trim();
            booking.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new BookingOperationResult { Success = true, Booking = booking };
        }

        public async Task<BookingOperationResult> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return new BookingOperationResult { Success = false, ErrorMessage = "Booking not found." };
            }

            booking.Status = newStatus;
            await _context.SaveChangesAsync();

            return new BookingOperationResult { Success = true, Booking = booking };
        }

        private static string GenerateBookingReference()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = new byte[5];
            RandomNumberGenerator.Fill(bytes);
            var code = new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
            return $"NZR-{DateTime.UtcNow.Year}-{code}";
        }
    }
}
