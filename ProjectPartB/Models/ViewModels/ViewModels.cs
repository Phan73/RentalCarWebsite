using System;
using System.Collections.Generic;
using ProjectPartB.Models;
using ProjectPartB.Services;

namespace ProjectPartB.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Location> Locations { get; set; } = new();
        public List<CarType> Categories { get; set; } = new();
        public List<CarDescription> FeaturedVehicles { get; set; } = new();
        public DateTime DefaultPickupDate { get; set; } = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        public DateTime DefaultReturnDate { get; set; } = DateTime.UtcNow.Date.AddDays(5).AddHours(10);
    }

    public class VehicleListViewModel
    {
        public VehicleSearchResult SearchResult { get; set; } = new();
        public List<CarType> Categories { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public VehicleSearchFilter Filter { get; set; } = new();
    }

    public class VehicleDetailViewModel
    {
        public CarDescription Vehicle { get; set; } = null!;
        public List<Location> Locations { get; set; } = new();
        public DateTime DefaultPickupDate { get; set; } = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        public DateTime DefaultReturnDate { get; set; } = DateTime.UtcNow.Date.AddDays(4).AddHours(10);
        public PricingBreakdownResult EstimatedPricing { get; set; } = null!;
    }

    public class BookingFormViewModel
    {
        public int CarDescriptionId { get; set; }
        public CarDescription? Vehicle { get; set; }
        public List<Location> Locations { get; set; } = new();

        public int PickupLocationId { get; set; }
        public int ReturnLocationId { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime ReturnDateTime { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string PaymentMethod { get; set; } = "Pay at Pickup";

        public bool IncludeGps { get; set; }
        public bool IncludeChildSeat { get; set; }
        public bool IncludeAdditionalDriver { get; set; }
        public bool IncludeZeroExcess { get; set; }

        public PricingBreakdownResult? CalculatedPricing { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public FleetStatisticsResult Stats { get; set; } = new();
        public List<Booking> RecentBookings { get; set; } = new();
        public List<CarDescription> Vehicles { get; set; } = new();
    }
}
