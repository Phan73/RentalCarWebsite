using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectPartB.Models;

namespace ProjectPartB.Services
{
    public class VehicleSearchFilter
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Transmission { get; set; }
        public string? FuelType { get; set; }
        public int? MinSeats { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? PickupDateTime { get; set; }
        public DateTime? ReturnDateTime { get; set; }
        public int? PickupLocationId { get; set; }
        public int? ReturnLocationId { get; set; }
        public string? SortBy { get; set; } // price_asc, price_desc, brand_asc, year_desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }

    public class VehicleSearchResult
    {
        public List<CarDescription> Vehicles { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class FleetStatisticsResult
    {
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int CurrentlyRentedVehicles { get; set; }
        public int InMaintenanceVehicles { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public interface IFleetService
    {
        Task<VehicleSearchResult> SearchVehiclesAsync(VehicleSearchFilter filter);
        Task<List<CarDescription>> GetFeaturedVehiclesAsync(int count = 6);
        Task<CarDescription?> GetVehicleDetailsAsync(int carDescriptionId);
        Task<List<CarType>> GetActiveCategoriesAsync();
        Task<List<Location>> GetAllLocationsAsync();
        Task<FleetStatisticsResult> GetFleetStatisticsAsync();
    }
}
