using System;
using System.Collections.Generic;
using ProjectPartB.Services;
using Xunit;

namespace ProjectPartB.Tests
{
    public class PricingServiceTests
    {
        private readonly IPricingService _pricingService;

        public PricingServiceTests()
        {
            _pricingService = new PricingService();
        }

        [Fact]
        public void CalculateRentalDays_SameDay_ReturnsMinimumOneDay()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 1, 15, 0, 0);

            var days = _pricingService.CalculateRentalDays(pickup, returnDate);

            Assert.Equal(1, days);
        }

        [Fact]
        public void CalculateRentalDays_ReturnBeforePickup_ReturnsMinimumOneDay()
        {
            var pickup = new DateTime(2026, 12, 5, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 1, 10, 0, 0);

            var days = _pricingService.CalculateRentalDays(pickup, returnDate);

            Assert.Equal(1, days);
        }

        [Fact]
        public void CalculateRentalDays_Exact72Hours_ReturnsThreeDays()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 4, 10, 0, 0);

            var days = _pricingService.CalculateRentalDays(pickup, returnDate);

            Assert.Equal(3, days);
        }

        [Fact]
        public void CalculateRentalDays_PartialDayOver24Hours_RoundsUpToNextDay()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 2, 14, 0, 0); // 28 hours = 2 days

            var days = _pricingService.CalculateRentalDays(pickup, returnDate);

            Assert.Equal(2, days);
        }

        [Fact]
        public void CalculateBookingPrice_Under7Days_NoDiscount()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 4, 10, 0, 0); // 3 days
            decimal rate = 100.00m;

            var result = _pricingService.CalculateBookingPrice(rate, pickup, returnDate);

            Assert.Equal(3, result.RentalDays);
            Assert.Equal(300.00m, result.GrossBaseCost);
            Assert.Equal(0m, result.DurationDiscountPercent);
            Assert.Equal(0m, result.DiscountAmount);
            Assert.Equal(300.00m, result.NetBaseCost);
            Assert.Equal(45.00m, result.TaxAmount); // 15% GST on $300
            Assert.Equal(345.00m, result.TotalAmount);
            Assert.Equal(250.00m, result.SecurityDeposit);
        }

        [Theory]
        [InlineData(7, 10.0)]
        [InlineData(14, 15.0)]
        [InlineData(30, 20.0)]
        public void CalculateBookingPrice_TieredDurationDiscounts_AppliedCorrectly(int rentalDays, decimal expectedDiscountPercent)
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = pickup.AddDays(rentalDays);
            decimal rate = 100.00m;

            var result = _pricingService.CalculateBookingPrice(rate, pickup, returnDate);

            Assert.Equal(expectedDiscountPercent, result.DurationDiscountPercent);
            var expectedGross = rate * rentalDays;
            var expectedDiscount = Math.Round(expectedGross * (expectedDiscountPercent / 100m), 2);
            var expectedNet = expectedGross - expectedDiscount;
            var expectedTax = Math.Round(expectedNet * 0.15m, 2);

            Assert.Equal(expectedDiscount, result.DiscountAmount);
            Assert.Equal(expectedNet, result.NetBaseCost);
            Assert.Equal(expectedTax, result.TaxAmount);
            Assert.Equal(expectedNet + expectedTax, result.TotalAmount);
        }

        [Fact]
        public void CalculateBookingPrice_WithExtras_CalculatesCorrectTotals()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 3, 10, 0, 0); // 2 days
            decimal rate = 80.00m;

            var extras = new List<PricingExtraRequest>
            {
                new PricingExtraRequest { Name = "GPS Navigation", DailyPrice = 10.00m, Quantity = 1 },
                new PricingExtraRequest { Name = "Child Safety Seat", DailyPrice = 12.00m, Quantity = 2 }
            };

            var result = _pricingService.CalculateBookingPrice(rate, pickup, returnDate, extras);

            // Gross = 2 * 80 = 160
            // GPS = 10 * 2 * 1 = 20
            // Child Seats = 12 * 2 * 2 = 48
            // Total extras = 68
            // Subtotal = 160 + 68 = 228
            // Tax = 228 * 0.15 = 34.20
            // Total = 262.20
            Assert.Equal(68.00m, result.ExtrasTotal);
            Assert.Equal(228.00m, result.SubTotal);
            Assert.Equal(34.20m, result.TaxAmount);
            Assert.Equal(262.20m, result.TotalAmount);
            Assert.Equal(2, result.Extras.Count);
        }

        [Fact]
        public void CalculateBookingPrice_ZeroExcessInsurance_WaivesSecurityDeposit()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 4, 10, 0, 0);

            var result = _pricingService.CalculateBookingPrice(100m, pickup, returnDate, null, hasZeroExcessInsurance: true);

            Assert.Equal(0.00m, result.SecurityDeposit);
        }

        [Fact]
        public void CalculateBookingPrice_NegativeDailyRate_ThrowsException()
        {
            var pickup = new DateTime(2026, 12, 1, 10, 0, 0);
            var returnDate = new DateTime(2026, 12, 4, 10, 0, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _pricingService.CalculateBookingPrice(-50m, pickup, returnDate));
        }
    }
}
