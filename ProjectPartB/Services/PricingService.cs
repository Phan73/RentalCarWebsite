using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPartB.Services
{
    public class PricingService : IPricingService
    {
        public const decimal DefaultTaxRate = 15.0m; // 15% NZ GST
        public const decimal StandardSecurityDeposit = 250.00m; // NZD

        public int CalculateRentalDays(DateTime pickupDate, DateTime returnDate)
        {
            if (returnDate <= pickupDate)
            {
                return 1;
            }

            var difference = returnDate - pickupDate;
            var totalHours = difference.TotalHours;
            // Provide a small grace period buffer (5 minutes = 0.083 hours) to prevent microsecond tick variance from bumping full-day increments
            var days = (int)Math.Ceiling(Math.Max(0, totalHours - 0.083) / 24.0);

            return Math.Max(1, days);
        }

        public PricingBreakdownResult CalculateBookingPrice(
            decimal dailyRate,
            DateTime pickupDate,
            DateTime returnDate,
            IEnumerable<PricingExtraRequest>? extras = null,
            bool hasZeroExcessInsurance = false)
        {
            if (dailyRate < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dailyRate), "Daily rate cannot be negative.");
            }

            var days = CalculateRentalDays(pickupDate, returnDate);

            // Duration discount schedule
            decimal discountPercent = 0m;
            if (days >= 30)
            {
                discountPercent = 20.0m;
            }
            else if (days >= 14)
            {
                discountPercent = 15.0m;
            }
            else if (days >= 7)
            {
                discountPercent = 10.0m;
            }

            var grossBaseCost = Math.Round(dailyRate * days, 2);
            var discountAmount = Math.Round(grossBaseCost * (discountPercent / 100.0m), 2);
            var netBaseCost = grossBaseCost - discountAmount;

            var extraBreakdowns = new List<PricingExtraBreakdown>();
            decimal extrasTotal = 0m;

            if (extras != null)
            {
                foreach (var extra in extras)
                {
                    if (extra.DailyPrice < 0 || extra.Quantity <= 0) continue;

                    var lineTotal = Math.Round(extra.DailyPrice * days * extra.Quantity, 2);
                    extrasTotal += lineTotal;
                    extraBreakdowns.Add(new PricingExtraBreakdown
                    {
                        Name = extra.Name,
                        DailyPrice = extra.DailyPrice,
                        Quantity = extra.Quantity,
                        TotalPrice = lineTotal
                    });
                }
            }

            var subTotal = netBaseCost + extrasTotal;
            var taxAmount = Math.Round(subTotal * (DefaultTaxRate / 100.0m), 2);
            var totalAmount = subTotal + taxAmount;

            // Security deposit is waived or reduced if zero-excess coverage is selected
            var deposit = hasZeroExcessInsurance ? 0.00m : StandardSecurityDeposit;

            return new PricingBreakdownResult
            {
                RentalDays = days,
                DailyRate = dailyRate,
                GrossBaseCost = grossBaseCost,
                DurationDiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                NetBaseCost = netBaseCost,
                ExtrasTotal = extrasTotal,
                SubTotal = subTotal,
                TaxRatePercent = DefaultTaxRate,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                SecurityDeposit = deposit,
                Currency = "NZD",
                Extras = extraBreakdowns
            };
        }
    }
}
