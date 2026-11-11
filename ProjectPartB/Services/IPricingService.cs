using System;
using System.Collections.Generic;

namespace ProjectPartB.Services
{
    public class PricingExtraRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal DailyPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class PricingBreakdownResult
    {
        public int RentalDays { get; set; }
        public decimal DailyRate { get; set; }
        public decimal GrossBaseCost { get; set; }
        public decimal DurationDiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetBaseCost { get; set; }
        public decimal ExtrasTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxRatePercent { get; set; } = 15.0m; // NZ GST
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
        public string Currency { get; set; } = "NZD";
        public List<PricingExtraBreakdown> Extras { get; set; } = new();
    }

    public class PricingExtraBreakdown
    {
        public string Name { get; set; } = string.Empty;
        public decimal DailyPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public interface IPricingService
    {
        int CalculateRentalDays(DateTime pickupDate, DateTime returnDate);
        PricingBreakdownResult CalculateBookingPrice(
            decimal dailyRate,
            DateTime pickupDate,
            DateTime returnDate,
            IEnumerable<PricingExtraRequest>? extras = null,
            bool hasZeroExcessInsurance = false);
    }
}
