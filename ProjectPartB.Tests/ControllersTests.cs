using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Controllers;
using ProjectPartB.Models;
using ProjectPartB.Models.ViewModels;
using ProjectPartB.Services;
using Xunit;

namespace ProjectPartB.Tests
{
    public class ControllersTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly FS23_Group4_ProjectContext _context;

        public ControllersTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FS23_Group4_ProjectContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new FS23_Group4_ProjectContext(options);
            _context.Database.EnsureCreated();

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var category = new CarType { Name = "Sedan", Description = "Premium Sedan" };
            _context.CarTypes.Add(category);
            _context.SaveChanges();

            var car = new CarDescription
            {
                CarType = category,
                CarTypeName = "Sedan",
                Brand = "Audi",
                Model = "A4",
                Color = "Black",
                RatePerDay = 130.00m,
                Available = true,
                Featured = true,
                Image = "audi.jpg",
                Description = "Luxury German Sedan"
            };

            var loc1 = new Location { Name = "Auckland Airport", City = "Auckland", Address = "Ray Emery Dr" };
            var loc2 = new Location { Name = "Wellington Airport", City = "Wellington", Address = "Stewart Duff Dr" };

            _context.CarDescriptions.Add(car);
            _context.Locations.AddRange(loc1, loc2);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task HomeController_Index_ReturnsViewWithHomeViewModel()
        {
            // Arrange
            var mockFleetService = new Mock<IFleetService>();
            mockFleetService.Setup(f => f.GetFeaturedVehiclesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CarDescription> { new CarDescription { Brand = "Audi", Model = "A4" } });
            mockFleetService.Setup(f => f.GetAllLocationsAsync())
                .ReturnsAsync(new List<Location> { new Location { Name = "Auckland Airport" } });
            mockFleetService.Setup(f => f.GetActiveCategoriesAsync())
                .ReturnsAsync(new List<CarType> { new CarType { Name = "Sedan" } });

            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockFleetService.Object, mockLogger.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.Model);
            Assert.Single(model.FeaturedVehicles);
            Assert.Single(model.Locations);
            Assert.Single(model.Categories);
        }

        [Fact]
        public async Task HomeController_Contact_ReturnsViewWithLocations()
        {
            var mockFleetService = new Mock<IFleetService>();
            mockFleetService.Setup(f => f.GetAllLocationsAsync())
                .ReturnsAsync(new List<Location>
                {
                    new Location { Name = "Auckland Airport" },
                    new Location { Name = "Christchurch Airport" }
                });

            var mockLogger = new Mock<ILogger<HomeController>>();
            var controller = new HomeController(mockFleetService.Object, mockLogger.Object);

            var result = await controller.Contact();

            var viewResult = Assert.IsType<ViewResult>(result);
            var locations = Assert.IsAssignableFrom<IEnumerable<Location>>(viewResult.Model);
            Assert.NotNull(locations);
        }

        [Fact]
        public async Task CarDescriptionsController_Details_NonExistentId_ReturnsNotFound()
        {
            var fleetService = new FleetService(_context);
            var pricingService = new PricingService();
            var mockEnv = new Mock<IWebHostEnvironment>();

            var controller = new CarDescriptionsController(_context, fleetService, pricingService, mockEnv.Object);

            var result = await controller.Details(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CarDescriptionsController_Details_ExistingId_ReturnsViewWithVehicle()
        {
            var fleetService = new FleetService(_context);
            var pricingService = new PricingService();
            var mockEnv = new Mock<IWebHostEnvironment>();

            var controller = new CarDescriptionsController(_context, fleetService, pricingService, mockEnv.Object);

            var result = await controller.Details(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VehicleDetailViewModel>(viewResult.Model);
            Assert.Equal("Audi", model.Vehicle.Brand);
            Assert.Equal("A4", model.Vehicle.Model);
            Assert.Equal(130.00m, model.Vehicle.RatePerDay);
        }

        [Fact]
        public async Task BookingController_CalculatePrice_ValidRequest_ReturnsPricingBreakdown()
        {
            var pricingService = new PricingService();
            var fleetService = new FleetService(_context);
            var bookingService = new BookingService(_context, pricingService);
            var mockEmail = new Mock<IEmailNotificationService>();
            var mockUserStore = new Mock<IUserStore<WebUser>>();
            var mockUserManager = new Mock<UserManager<WebUser>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var controller = new BookingController(
                bookingService,
                fleetService,
                pricingService,
                mockEmail.Object,
                mockUserManager.Object);

            var baseDate = DateTime.UtcNow.Date.AddDays(2);
            var request = new LivePriceCalculationRequest
            {
                CarId = 1,
                PickupDateTime = baseDate.AddHours(10),
                ReturnDateTime = baseDate.AddDays(3).AddHours(10),
                IncludeGps = true,
                IncludeChildSeat = false,
                IncludeAdditionalDriver = false,
                IncludeZeroExcess = true
            };

            var actionResult = await controller.CalculatePrice(request);

            var jsonResult = Assert.IsType<JsonResult>(actionResult);
            Assert.NotNull(jsonResult.Value);

            var pricing = Assert.IsType<PricingBreakdownResult>(jsonResult.Value);
            Assert.Equal(3, pricing.RentalDays);
            Assert.True(pricing.TotalAmount > 0);
            Assert.Equal(0m, pricing.SecurityDeposit); // Zero excess insurance reduces bond to $0
        }

        [Fact]
        public async Task BookingController_CalculatePrice_ReturnBeforePickup_ReturnsBadRequest()
        {
            var pricingService = new PricingService();
            var fleetService = new FleetService(_context);
            var bookingService = new BookingService(_context, pricingService);
            var mockEmail = new Mock<IEmailNotificationService>();
            var mockUserStore = new Mock<IUserStore<WebUser>>();
            var mockUserManager = new Mock<UserManager<WebUser>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var controller = new BookingController(
                bookingService,
                fleetService,
                pricingService,
                mockEmail.Object,
                mockUserManager.Object);

            var request = new LivePriceCalculationRequest
            {
                CarId = 1,
                PickupDateTime = DateTime.UtcNow.AddDays(5),
                ReturnDateTime = DateTime.UtcNow.AddDays(2) // Invalid: return before pickup
            };

            var actionResult = await controller.CalculatePrice(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal(400, badRequest.StatusCode);
        }
    }
}
