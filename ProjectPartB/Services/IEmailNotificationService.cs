using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public interface IEmailNotificationService : IEmailSender
    {
        Task SendBookingConfirmationAsync(Booking booking);
        Task SendBookingCancellationAsync(Booking booking);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(ILogger<EmailNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Robust configurable logger/mock for transactional emails without hardcoding external SMTP credentials
            _logger.LogInformation("Sending email to {Email} | Subject: {Subject}", email, subject);
            return Task.CompletedTask;
        }

        public Task SendBookingConfirmationAsync(Booking booking)
        {
            var subject = $"Booking Confirmation — {booking.BookingReference} | Apex Luxury Car Rentals";
            var body = $@"
                <h2>Thank you for your reservation, {booking.CustomerName}!</h2>
                <p>Your booking <strong>{booking.BookingReference}</strong> has been confirmed.</p>
                <p><strong>Vehicle:</strong> {booking.CarDescription?.Brand} {booking.CarDescription?.Model} ({booking.CarDescription?.Year})</p>
                <p><strong>Pickup:</strong> {booking.PickupDateTime:dd MMM yyyy, HH:mm} at {booking.PickupLocation?.Name}</p>
                <p><strong>Return:</strong> {booking.ReturnDateTime:dd MMM yyyy, HH:mm} at {booking.ReturnLocation?.Name}</p>
                <p><strong>Total Amount:</strong> ${booking.TotalAmount:N2} NZD</p>
                <p>Please present your valid Driver's Licence ({booking.DriverLicenseNumber}) upon vehicle collection.</p>";

            return SendEmailAsync(booking.CustomerEmail, subject, body);
        }

        public Task SendBookingCancellationAsync(Booking booking)
        {
            var subject = $"Booking Cancellation Notice — {booking.BookingReference}";
            var body = $@"
                <h2>Reservation Cancellation</h2>
                <p>Dear {booking.CustomerName}, your booking <strong>{booking.BookingReference}</strong> has been cancelled.</p>
                <p><strong>Reason:</strong> {booking.CancellationReason ?? "Customer request"}</p>
                <p>If you have questions, please contact our support team.</p>";

            return SendEmailAsync(booking.CustomerEmail, subject, body);
        }
    }
}
