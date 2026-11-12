using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public class FleetService : IFleetService
    {
        private readonly FS23_Group4_ProjectContext _context;

        public FleetService(FS23_Group4_ProjectContext context)
        {
            _context = context;
        }

        public async Task<VehicleSearchResult> SearchVehiclesAsync(VehicleSearchFilter filter)
        {
            var query = _context.CarDescriptions
                .Include(c => c.CarType)
                .AsQueryable();

            // Text search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c => (c.Brand != null && c.Brand.ToLower().Contains(term))
                                      || (c.Model != null && c.Model.ToLower().Contains(term))
                                      || (c.Description != null && c.Description.ToLower().Contains(term))
                                      || (c.CarTypeName != null && c.CarTypeName.ToLower().Contains(term)));
            }

            // Category filter
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(c => c.CarTypeId == filter.CategoryId.Value);
            }

            // Transmission filter
            if (!string.IsNullOrWhiteSpace(filter.Transmission))
            {
                query = query.Where(c => c.Transmission == filter.Transmission);
            }

            // Fuel type filter
            if (!string.IsNullOrWhiteSpace(filter.FuelType))
            {
                query = query.Where(c => c.FuelType == filter.FuelType);
            }

            // Seats filter
            if (filter.MinSeats.HasValue && filter.MinSeats.Value > 0)
            {
                query = query.Where(c => c.Seats >= filter.MinSeats.Value);
            }

            // Price range filters
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(c => c.RatePerDay >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(c => c.RatePerDay <= filter.MaxPrice.Value);
            }

            // Availability by date range:
            if (filter.PickupDateTime.HasValue && filter.ReturnDateTime.HasValue && filter.ReturnDateTime > filter.PickupDateTime)
            {
                var pDate = filter.PickupDateTime.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.PickupDateTime.Value, DateTimeKind.Utc)
                    : filter.PickupDateTime.Value.ToUniversalTime();

                var rDate = filter.ReturnDateTime.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.ReturnDateTime.Value, DateTimeKind.Utc)
                    : filter.ReturnDateTime.Value.ToUniversalTime();

                // Find car IDs that have active overlapping bookings
                var bookedCarIds = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled
                             && b.PickupDateTime < rDate
                             && b.ReturnDateTime > pDate)
                    .Select(b => b.CarDescriptionId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(c => c.Available == true && !bookedCarIds.Contains(c.CarDescriptionId));
            }
            else
            {
                // General availability
                query = query.Where(c => c.Available == true);
            }

            // Sorting (cast decimal to double for broad provider compatibility including SQLite in-memory tests)
            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(c => (double)c.RatePerDay),
                "price_desc" => query.OrderByDescending(c => (double)c.RatePerDay),
                "brand_asc" => query.OrderBy(c => c.Brand).ThenBy(c => c.Model),
                "year_desc" => query.OrderByDescending(c => c.Year),
                _ => query.OrderByDescending(c => c.Featured).ThenBy(c => (double)c.RatePerDay)
            };

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 50);

            var vehicles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new VehicleSearchResult
            {
                Vehicles = vehicles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<CarDescription>> GetFeaturedVehiclesAsync(int count = 6)
        {
            return await _context.CarDescriptions
                .Include(c => c.CarType)
                .Where(c => c.Available == true)
                .OrderByDescending(c => c.Featured)
                .ThenBy(c => (double)c.RatePerDay)
                .Take(count)
                .ToListAsync();
        }

        public async Task<CarDescription?> GetVehicleDetailsAsync(int carDescriptionId)
        {
            return await _context.CarDescriptions
                .Include(c => c.CarType)
                .Include(c => c.MaintenanceRecords)
                .FirstOrDefaultAsync(c => c.CarDescriptionId == carDescriptionId);
        }

        public async Task<List<CarType>> GetActiveCategoriesAsync()
        {
            return await _context.CarTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Location>> GetAllLocationsAsync()
        {
            return await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<FleetStatisticsResult> GetFleetStatisticsAsync()
        {
            var totalVehicles = await _context.CarDescriptions.CountAsync();
            var availableVehicles = await _context.CarDescriptions.CountAsync(c => c.Available == true);
            var now = DateTime.UtcNow;

            var activeBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Active
                             || (b.Status == BookingStatus.Confirmed && b.PickupDateTime <= now && b.ReturnDateTime >= now));

            var upcomingBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Confirmed && b.PickupDateTime > now);

            var completedBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Completed);

            var cancelledBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Cancelled);

            var totalBookings = await _context.Bookings.CountAsync();

            var revenueAmounts = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Select(b => b.TotalAmount)
                .ToListAsync();
            var totalRevenue = revenueAmounts.Sum();

            var inMaintenance = await _context.MaintenanceRecords
                .CountAsync(m => !m.IsResolved);

            return new FleetStatisticsResult
            {
                TotalVehicles = totalVehicles,
                AvailableVehicles = availableVehicles,
                CurrentlyRentedVehicles = activeBookings,
                InMaintenanceVehicles = inMaintenance,
                TotalBookings = totalBookings,
                ActiveBookings = activeBookings,
                UpcomingBookings = upcomingBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue
            };
        }
    }
}
