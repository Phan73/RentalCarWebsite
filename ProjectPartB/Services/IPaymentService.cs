using System;
using System.Threading.Tasks;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public class PaymentProcessRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NZD";
        public string PaymentMethod { get; set; } = "Credit Card (Simulated)";
        public string? CardNumberMasked { get; set; }
        public string? CardHolderName { get; set; }
    }

    public class PaymentProcessResult
    {
        public bool Succeeded { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request);
        Task<bool> RefundPaymentAsync(string transactionReference, decimal amount, string reason);
    }

    public class PaymentService : IPaymentService
    {
        public Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            if (request.Amount <= 0)
            {
                return Task.FromResult(new PaymentProcessResult
                {
                    Succeeded = false,
                    ErrorMessage = "Payment amount must be greater than zero."
                });
            }

            // Commercial grade provider-independent abstraction
            // Supports Pay on Pickup, Simulated Card, and easily wires to Stripe/Windcave
            var txnRef = "PAY-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            return Task.FromResult(new PaymentProcessResult
            {
                Succeeded = true,
                TransactionReference = txnRef,
                Status = request.PaymentMethod == "Pay at Pickup" ? "Pending" : "Paid"
            });
        }

        public Task<bool> RefundPaymentAsync(string transactionReference, decimal amount, string reason)
        {
            // Refund simulation log
            return Task.FromResult(true);
        }
    }
}
