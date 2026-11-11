using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public class BookingCreateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public int CarDescriptionId { get; set; }
        public int PickupLocationId { get; set; }
        public int ReturnLocationId { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime ReturnDateTime { get; set; }
        public string? CustomerNotes { get; set; }
        public string PaymentMethod { get; set; } = "Pay at Pickup";
        public List<PricingExtraRequest> SelectedExtras { get; set; } = new();
    }

    public class BookingOperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Booking? Booking { get; set; }
    }

    public interface IBookingService
    {
        Task<bool> IsVehicleAvailableAsync(int carDescriptionId, DateTime pickup, DateTime returnDate, int? excludeBookingId = null);
        Task<BookingOperationResult> CreateBookingAsync(BookingCreateRequest request);
        Task<Booking?> GetBookingByReferenceAsync(string reference);
        Task<Booking?> GetBookingByIdAsync(int id);
        Task<List<Booking>> GetUserBookingsAsync(string userId);
        Task<List<Booking>> GetAllBookingsAsync(BookingStatus? status = null, string? search = null);
        Task<BookingOperationResult> CancelBookingAsync(int bookingId, string userId, string reason, bool isAdmin = false);
        Task<BookingOperationResult> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus);
    }
}
