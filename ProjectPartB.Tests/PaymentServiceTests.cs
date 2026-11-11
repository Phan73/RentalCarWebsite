using System.Threading.Tasks;
using ProjectPartB.Services;
using Xunit;

namespace ProjectPartB.Tests
{
    public class PaymentServiceTests
    {
        private readonly IPaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentService = new PaymentService();
        }

        [Fact]
        public async Task ProcessPayment_ValidAmount_GeneratesSuccessfulTransaction()
        {
            var request = new PaymentProcessRequest
            {
                BookingId = 101,
                Amount = 350.00m,
                Currency = "NZD",
                PaymentMethod = "Credit Card (Simulated)"
            };

            var result = await _paymentService.ProcessPaymentAsync(request);

            Assert.True(result.Succeeded);
            Assert.StartsWith("PAY-", result.TransactionReference);
            Assert.Equal("Paid", result.Status);
        }

        [Fact]
        public async Task ProcessPayment_PayAtPickup_StatusIsPending()
        {
            var request = new PaymentProcessRequest
            {
                BookingId = 102,
                Amount = 250.00m,
                PaymentMethod = "Pay at Pickup"
            };

            var result = await _paymentService.ProcessPaymentAsync(request);

            Assert.True(result.Succeeded);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task ProcessPayment_ZeroOrNegativeAmount_FailsValidation()
        {
            var request = new PaymentProcessRequest
            {
                BookingId = 103,
                Amount = -50.00m
            };

            var result = await _paymentService.ProcessPaymentAsync(request);

            Assert.False(result.Succeeded);
            Assert.Contains("must be greater than zero", result.ErrorMessage);
        }
    }
}
