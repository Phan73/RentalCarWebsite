using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;
using ProjectPartB.Services;
using Xunit;

namespace ProjectPartB.Tests
{
    public class BookingServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly FS23_Group4_ProjectContext _context;
        private readonly IPricingService _pricingService;
        private readonly IBookingService _bookingService;

        public BookingServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FS23_Group4_ProjectContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new FS23_Group4_ProjectContext(options);
            _context.Database.EnsureCreated();

            _pricingService = new PricingService();
            _bookingService = new BookingService(_context, _pricingService);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var category = new CarType { Name = "SUV", Description = "Sport Utility Vehicle" };
            _context.CarTypes.Add(category);
            _context.SaveChanges();

            var vehicle = new CarDescription
            {
                CarType = category,
                CarTypeName = "SUV",
                Brand = "Toyota",
                Model = "RAV4",
                Color = "Silver",
                RatePerDay = 90.00m,
                Available = true,
                Image = "toyota.jpg",
                Description = "Reliable Touring SUV"
            };

            var pickupLoc = new Location { Name = "Auckland Airport", Address = "Ray Emery Dr", City = "Auckland" };
            var returnLoc = new Location { Name = "Christchurch Airport", Address = "Durey Rd", City = "Christchurch" };

            _context.CarDescriptions.Add(vehicle);
            _context.Locations.AddRange(pickupLoc, returnLoc);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task CreateBooking_ValidRequest_CreatesConfirmedBookingWithReference()
        {
            var baseDate = DateTime.UtcNow.Date;
            var pickup = baseDate.AddDays(2).AddHours(10);
            var returnDate = baseDate.AddDays(6).AddHours(10);

            var request = new BookingCreateRequest
            {
                UserId = "user-123",
                CustomerName = "John Doe",
                CustomerEmail = "john@example.co.nz",
                CustomerPhone = "+64 21 111 2222",
                DriverLicenseNumber = "NZD-998811",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 2,
                PickupDateTime = pickup,
                ReturnDateTime = returnDate,
                PaymentMethod = "Pay at Pickup"
            };

            var result = await _bookingService.CreateBookingAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(result.Booking);
            Assert.StartsWith("NZR-", result.Booking.BookingReference);
            Assert.Equal(BookingStatus.Confirmed, result.Booking.Status);
            Assert.Equal(4, result.Booking.RentalDays);
            Assert.NotNull(result.Booking.Payment);
            Assert.Equal(result.Booking.TotalAmount, result.Booking.Payment.Amount);
        }

        [Fact]
        public async Task DoubleBookingPrevention_OverlappingInterval_FailsReservation()
        {
            // Initial booking from Day 5 to Day 10
            var existingPickup = DateTime.UtcNow.AddDays(5);
            var existingReturn = DateTime.UtcNow.AddDays(10);

            var initialRequest = new BookingCreateRequest
            {
                UserId = "user-1",
                CustomerName = "Alice Smith",
                CustomerEmail = "alice@example.com",
                CustomerPhone = "021123456",
                DriverLicenseNumber = "DL-112233",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 1,
                PickupDateTime = existingPickup,
                ReturnDateTime = existingReturn
            };

            var firstResult = await _bookingService.CreateBookingAsync(initialRequest);
            Assert.True(firstResult.Success);

            // Attempt overlapping booking from Day 7 to Day 12
            var overlappingRequest = new BookingCreateRequest
            {
                UserId = "user-2",
                CustomerName = "Bob Brown",
                CustomerEmail = "bob@example.com",
                CustomerPhone = "021987654",
                DriverLicenseNumber = "DL-445566",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(7),
                ReturnDateTime = DateTime.UtcNow.AddDays(12)
            };

            var secondResult = await _bookingService.CreateBookingAsync(overlappingRequest);

            Assert.False(secondResult.Success);
            Assert.Contains("reserved by another customer", secondResult.ErrorMessage);
        }

        [Fact]
        public async Task IsVehicleAvailable_NonOverlappingDates_ReturnsTrue()
        {
            // Existing booking from Day 10 to Day 15
            var booking = new Booking
            {
                BookingReference = "NZR-2026-TEST1",
                UserId = "user-1",
                CustomerName = "Test",
                CustomerEmail = "test@example.com",
                CustomerPhone = "021000000",
                DriverLicenseNumber = "DL123",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(10),
                ReturnDateTime = DateTime.UtcNow.AddDays(15),
                Status = BookingStatus.Confirmed
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Check non-overlapping window: Day 2 to Day 8
            var isAvailableBefore = await _bookingService.IsVehicleAvailableAsync(1, DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(8));
            Assert.True(isAvailableBefore);

            // Check non-overlapping window: Day 16 to Day 20
            var isAvailableAfter = await _bookingService.IsVehicleAvailableAsync(1, DateTime.UtcNow.AddDays(16), DateTime.UtcNow.AddDays(20));
            Assert.True(isAvailableAfter);
        }

        [Fact]
        public async Task CancelBooking_ActiveBooking_TransitionsToCancelled()
        {
            var booking = new Booking
            {
                BookingReference = "NZR-2026-CANCEL",
                UserId = "customer-10",
                CustomerName = "David Lee",
                CustomerEmail = "david@example.co.nz",
                CustomerPhone = "021555123",
                DriverLicenseNumber = "NZ-332211",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(5),
                ReturnDateTime = DateTime.UtcNow.AddDays(9),
                Status = BookingStatus.Confirmed
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var cancelResult = await _bookingService.CancelBookingAsync(booking.BookingId, "customer-10", "Flight schedule change");

            Assert.True(cancelResult.Success);
            Assert.Equal(BookingStatus.Cancelled, cancelResult.Booking?.Status);
            Assert.Equal("Flight schedule change", cancelResult.Booking?.CancellationReason);
            Assert.NotNull(cancelResult.Booking?.CancelledAt);

            // Once cancelled, the vehicle window should be available again!
            var isAvailableNow = await _bookingService.IsVehicleAvailableAsync(1, DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(9));
            Assert.True(isAvailableNow);
        }

        [Fact]
        public async Task CreateBooking_ReturnBeforePickup_ReturnsError()
        {
            var request = new BookingCreateRequest
            {
                UserId = "user-test",
                CustomerName = "Jane Doe",
                CustomerEmail = "jane@example.com",
                CustomerPhone = "021999999",
                DriverLicenseNumber = "DL-999",
                CarDescriptionId = 1,
                PickupLocationId = 1,
                ReturnLocationId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(5),
                ReturnDateTime = DateTime.UtcNow.AddDays(2) // Invalid!
            };

            var result = await _bookingService.CreateBookingAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Return date and time must be after", result.ErrorMessage);
        }
    }
}
