using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Models;
using ProjectPartB.Services;
using Xunit;

namespace ProjectPartB.Tests
{
    public class FleetServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly FS23_Group4_ProjectContext _context;
        private readonly IFleetService _fleetService;

        public FleetServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FS23_Group4_ProjectContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new FS23_Group4_ProjectContext(options);
            _context.Database.EnsureCreated();

            _fleetService = new FleetService(_context);

            SeedData();
        }

        private void SeedData()
        {
            var sedan = new CarType { Name = "Sedan" };
            var suv = new CarType { Name = "SUV" };

            _context.CarTypes.AddRange(sedan, suv);
            _context.SaveChanges();

            _context.CarDescriptions.AddRange(
                new CarDescription
                {
                    CarType = sedan,
                    CarTypeName = "Sedan",
                    Brand = "Tesla",
                    Model = "Model 3",
                    Color = "Pearl White",
                    Image = "tesla.jpg",
                    RatePerDay = 120.00m,
                    Transmission = "Automatic",
                    FuelType = "Electric",
                    Seats = 5,
                    Year = 2024,
                    Available = true,
                    Featured = true,
                    Description = "Electric Sedan"
                },
                new CarDescription
                {
                    CarType = suv,
                    CarTypeName = "SUV",
                    Brand = "Toyota",
                    Model = "RAV4",
                    Color = "Silver",
                    Image = "toyota.jpg",
                    RatePerDay = 90.00m,
                    Transmission = "Automatic",
                    FuelType = "Hybrid",
                    Seats = 5,
                    Year = 2023,
                    Available = true,
                    Featured = false,
                    Description = "Hybrid SUV"
                },
                new CarDescription
                {
                    CarType = suv,
                    CarTypeName = "SUV",
                    Brand = "Hyundai",
                    Model = "Staria",
                    Color = "Black",
                    Image = "staria.jpg",
                    RatePerDay = 140.00m,
                    Transmission = "Automatic",
                    FuelType = "Diesel",
                    Seats = 8,
                    Year = 2024,
                    Available = false,
                    Featured = false,
                    Description = "8-seater People Mover"
                }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task SearchVehicles_ByBrandKeyword_ReturnsMatchingVehicles()
        {
            var filter = new VehicleSearchFilter { SearchTerm = "Tesla" };
            var result = await _fleetService.SearchVehiclesAsync(filter);

            Assert.Single(result.Vehicles);
            Assert.Equal("Tesla", result.Vehicles[0].Brand);
        }

        [Fact]
        public async Task SearchVehicles_FilterAvailableOnly_ExcludesUnavailable()
        {
            var filter = new VehicleSearchFilter();
            var result = await _fleetService.SearchVehiclesAsync(filter);

            Assert.Equal(2, result.TotalCount); // Hyundai is Available=false
            Assert.DoesNotContain(result.Vehicles, c => c.Brand == "Hyundai");
        }

        [Fact]
        public async Task SearchVehicles_SortByPriceAsc_ReturnsCheapestFirst()
        {
            var filter = new VehicleSearchFilter { SortBy = "price_asc" };
            var result = await _fleetService.SearchVehiclesAsync(filter);

            Assert.Equal(2, result.Vehicles.Count);
            Assert.Equal("Toyota", result.Vehicles[0].Brand); // $90
            Assert.Equal("Tesla", result.Vehicles[1].Brand);  // $120
        }

        [Fact]
        public async Task GetFeaturedVehicles_ReturnsOnlyFeaturedAndAvailable()
        {
            var featured = await _fleetService.GetFeaturedVehiclesAsync();

            Assert.NotEmpty(featured);
            Assert.All(featured, f => Assert.True(f.Available));
        }

        [Fact]
        public async Task GetFleetStatistics_CalculatesAccurateTotals()
        {
            var stats = await _fleetService.GetFleetStatisticsAsync();

            Assert.Equal(3, stats.TotalVehicles);
            Assert.Equal(2, stats.AvailableVehicles);
        }
    }
}
