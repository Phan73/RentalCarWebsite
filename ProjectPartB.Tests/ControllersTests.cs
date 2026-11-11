using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

        [Fact]
        public void DataProtection_CrossInstanceDecryptionSucceeds()
        {
            var masterKeyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<key id=""6cac850f-ac76-40ea-8c02-23f5afa16ed7"" version=""1"">
  <creationDate>2026-11-11T00:00:00.0000000Z</creationDate>
  <activationDate>2026-11-11T00:00:00.0000000Z</activationDate>
  <expirationDate>2036-12-31T23:59:59.0000000Z</expirationDate>
  <descriptor deserializerType=""Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"">
    <descriptor>
      <encryption algorithm=""AES_256_CBC"" />
      <validation algorithm=""HMACSHA256"" />
      <masterKey p4:requiresEncryption=""true"" xmlns:p4=""http://schemas.asp.net/2015/03/dataProtection"">
        <value>nDbEuP9p0oKbcyZIKmCVdfVHFqh26EE0SOHdoniLTCQjM2KQeIUDyUjSN8jiNbIYTCn0v6bKfWNmWu9WYIQKkg==</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>";

            var dir1 = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "dp_inst1_" + Guid.NewGuid()));
            var dir2 = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "dp_inst2_" + Guid.NewGuid()));
            dir1.Create();
            dir2.Create();
            File.WriteAllText(Path.Combine(dir1.FullName, "key-6cac850f.xml"), masterKeyXml);
            File.WriteAllText(Path.Combine(dir2.FullName, "key-6cac850f.xml"), masterKeyXml);

            var provider1 = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create(dir1);
            var provider2 = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create(dir2);

            var protector1 = provider1.CreateProtector("IdentityCookiePurpose");
            var protector2 = provider2.CreateProtector("IdentityCookiePurpose");

            var payload = System.Text.Encoding.UTF8.GetBytes("AdminAuthTicketData");
            var protectedByInstance1 = protector1.Protect(payload);

            var decryptedByInstance2 = protector2.Unprotect(protectedByInstance1);
            Assert.Equal("AdminAuthTicketData", System.Text.Encoding.UTF8.GetString(decryptedByInstance2));

            Directory.Delete(dir1.FullName, true);
            Directory.Delete(dir2.FullName, true);
        }

        [Fact]
        public async Task CarDescriptionsController_Create_WithImageFile_SuccessfullySavesVehicle()
        {
            var fleetService = new FleetService(_context);
            var pricingService = new PricingService();
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var controller = new CarDescriptionsController(_context, fleetService, pricingService, mockEnv.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var mockFile = new Mock<IFormFile>();
            var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
            mockFile.Setup(f => f.FileName).Returns("toyota.jpg");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) => target.WriteAsync(content, 0, content.Length));

            var newCar = new CarDescription
            {
                Brand = "Toyota",
                Model = "RAV4 Hybrid",
                CarTypeId = 1,
                RatePerDay = 115.00m,
                Color = "Pearl White Multi-Coat",
                Description = "Spacious all-wheel drive SUV",
                Seats = 5,
                Luggage = 3,
                Year = 2024
            };

            var result = await controller.Create(newCar, mockFile.Object);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CarDescriptionsController.Index), redirect.ActionName);

            var saved = await _context.CarDescriptions.FirstOrDefaultAsync(c => c.Brand == "Toyota");
            Assert.NotNull(saved);
            Assert.Equal("RAV4 Hybrid", saved.Model);
            Assert.Equal("Sedan", saved.CarTypeName); // Matched from CarTypeId = 1
            Assert.StartsWith("data:image/jpeg;base64,", saved.Image);
        }

        [Fact]
        public async Task CarDescriptionsController_Edit_WithNewImageFile_UpdatesVehicleAndImage()
        {
            var fleetService = new FleetService(_context);
            var pricingService = new PricingService();
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var controller = new CarDescriptionsController(_context, fleetService, pricingService, mockEnv.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var mockFile = new Mock<IFormFile>();
            var content = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic bytes
            mockFile.Setup(f => f.FileName).Returns("updated-audi.png");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) => target.WriteAsync(content, 0, content.Length));

            var existingCar = await _context.CarDescriptions.FirstAsync(c => c.Brand == "Audi");
            existingCar.RatePerDay = 145.00m;
            existingCar.Color = "Daytona Gray Pearl Effect"; // Long realistic color name > 20 chars

            var result = await controller.Edit(existingCar.CarDescriptionId, existingCar, mockFile.Object);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CarDescriptionsController.Index), redirect.ActionName);

            var updated = await _context.CarDescriptions.FindAsync(existingCar.CarDescriptionId);
            Assert.NotNull(updated);
            Assert.Equal(145.00m, updated.RatePerDay);
            Assert.Equal("Daytona Gray Pearl Effect", updated.Color);
            Assert.StartsWith("data:image/png;base64,", updated.Image);
        }

        [Fact]
        public async Task CarDescriptionsController_Edit_WithoutNewImage_PreservesExistingImage()
        {
            var fleetService = new FleetService(_context);
            var pricingService = new PricingService();
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var controller = new CarDescriptionsController(_context, fleetService, pricingService, mockEnv.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var existingCar = await _context.CarDescriptions.FirstAsync(c => c.Brand == "Audi");
            var originalImage = existingCar.Image;
            existingCar.Model = "A4 Avant Quattro";

            var result = await controller.Edit(existingCar.CarDescriptionId, existingCar, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CarDescriptionsController.Index), redirect.ActionName);

            var updated = await _context.CarDescriptions.FindAsync(existingCar.CarDescriptionId);
            Assert.NotNull(updated);
            Assert.Equal("A4 Avant Quattro", updated.Model);
            Assert.Equal(originalImage, updated.Image); // Preserved!
        }
    }
}
