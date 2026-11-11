using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Models;

namespace ProjectPartB.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            FS23_Group4_ProjectContext projectContext,
            CustomIdentityContext identityContext,
            RoleManager<IdentityRole> roleManager,
            UserManager<WebUser> userManager)
        {
            // Ensure database schemas exist for both contexts without conflicting
            var identityCreator = identityContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            var projectCreator = projectContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;

            if (identityCreator != null && !await identityCreator.ExistsAsync())
            {
                await identityCreator.CreateAsync();
            }

            // 1. Ensure Identity schemas exist
            bool identityTablesExist = false;
            try
            {
                identityTablesExist = await identityContext.Roles.AnyAsync() || true;
            }
            catch
            {
                identityTablesExist = false;
            }

            if (!identityTablesExist && identityCreator != null)
            {
                try
                {
                    await identityCreator.CreateTablesAsync();
                }
                catch
                {
                    var script = identityContext.Database.GenerateCreateScript();
                    await ExecuteScriptSafelyAsync(identityContext.Database, script);
                }
            }

            // 2. Ensure Project schemas exist
            bool projectTablesExist = false;
            try
            {
                projectTablesExist = await projectContext.CarDescriptions.AnyAsync() || true;
            }
            catch
            {
                projectTablesExist = false;
            }

            if (!projectTablesExist && projectCreator != null)
            {
                try
                {
                    await projectCreator.CreateTablesAsync();
                }
                catch
                {
                    var script = projectContext.Database.GenerateCreateScript();
                    await ExecuteScriptSafelyAsync(projectContext.Database, script);
                }
            }

            // 1. Seed Roles
            string[] roles = { "Administrator", "Manager", "Staff", "Customer", "Visitor", "Editor" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Default Admin User if none exists
            var adminEmail = "admin@apexcarrentals.co.nz";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new WebUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Apex Operations Lead",
                    Address = "100 Queen Street, Auckland CBD",
                    Phone = "+64 9 300 1234",
                    DrivingLicense = "DL-NZ-982341",
                    EmailConfirmed = true
                };

                var createRes = await userManager.CreateAsync(adminUser, "P@ssword2026!");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                    await userManager.AddToRoleAsync(adminUser, "Manager");
                }
            }

            // 3. Seed Default Customer if none exists
            var customerEmail = "customer@example.co.nz";
            var existingCustomer = await userManager.FindByEmailAsync(customerEmail);
            if (existingCustomer == null)
            {
                var customer = new WebUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FullName = "Emma Walker",
                    Address = "45 Victoria Avenue, Remuera, Auckland",
                    Phone = "+64 21 555 7890",
                    DrivingLicense = "NZL-774412",
                    EmailConfirmed = true
                };

                var custRes = await userManager.CreateAsync(customer, "Customer@123!");
                if (custRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(customer, "Customer");
                }
            }

            // 4. Seed Locations
            if (!await projectContext.Locations.AnyAsync())
            {
                var locations = new List<Location>
                {
                    new Location
                    {
                        Name = "Auckland Airport (AKL)",
                        Address = "Ray Emery Drive, Mangere",
                        City = "Auckland",
                        Phone = "+64 9 275 0789",
                        IsActive = true
                    },
                    new Location
                    {
                        Name = "Auckland Central CBD",
                        Address = "152 Hobson Street, Auckland Central",
                        City = "Auckland",
                        Phone = "+64 9 303 1122",
                        IsActive = true
                    },
                    new Location
                    {
                        Name = "Wellington Airport (WLG)",
                        Address = "Stewart Duff Drive, Rongotai",
                        City = "Wellington",
                        Phone = "+64 4 388 0500",
                        IsActive = true
                    },
                    new Location
                    {
                        Name = "Christchurch Airport (CHC)",
                        Address = "30 Durey Road, Christchurch Airport",
                        City = "Christchurch",
                        Phone = "+64 3 358 8123",
                        IsActive = true
                    },
                    new Location
                    {
                        Name = "Queenstown Airport (ZQN)",
                        Address = "Sir Henry Wigley Drive, Frankton",
                        City = "Queenstown",
                        Phone = "+64 3 442 3510",
                        IsActive = true
                    }
                };

                await projectContext.Locations.AddRangeAsync(locations);
                await projectContext.SaveChangesAsync();
            }

            // 5. Seed Car Types / Categories
            if (!await projectContext.CarTypes.AnyAsync())
            {
                var categories = new List<CarType>
                {
                    new CarType { Name = "Executive Sedan", Description = "Refined comfort, aerodynamic performance, and effortless motorway cruising." },
                    new CarType { Name = "Premium SUV", Description = "All-wheel drive capability and commanding visibility for scenic New Zealand road trips." },
                    new CarType { Name = "Electric & Hybrid", Description = "Zero or ultra-low emission driving with smooth instant torque and great range." },
                    new CarType { Name = "Luxury & Performance", Description = "Crafted interiors, sports suspension, and an unforgettable driving experience." },
                    new CarType { Name = "People Mover & Van", Description = "Spacious 7 to 8-passenger layouts with abundant luggage room for families and groups." }
                };

                await projectContext.CarTypes.AddRangeAsync(categories);
                await projectContext.SaveChangesAsync();
            }

            // 6. Seed High Quality Vehicles if catalog is sparse
            var carCount = await projectContext.CarDescriptions.CountAsync();
            if (carCount < 5)
            {
                var categories = await projectContext.CarTypes.ToListAsync();
                var sedanCat = categories.FirstOrDefault(c => c.Name.Contains("Sedan")) ?? categories.First();
                var suvCat = categories.FirstOrDefault(c => c.Name.Contains("SUV")) ?? categories.First();
                var evCat = categories.FirstOrDefault(c => c.Name.Contains("Electric")) ?? categories.First();
                var luxCat = categories.FirstOrDefault(c => c.Name.Contains("Luxury")) ?? categories.First();
                var vanCat = categories.FirstOrDefault(c => c.Name.Contains("Van")) ?? categories.First();

                var fleet = new List<CarDescription>
                {
                    new CarDescription
                    {
                        Brand = "Tesla",
                        Model = "Model 3 Long Range",
                        CarTypeId = evCat.Id,
                        CarTypeName = evCat.Name,
                        Color = "Pearl White Multi-Coat",
                        RatePerDay = 119.00m,
                        Transmission = "Automatic",
                        FuelType = "Electric",
                        Seats = 5,
                        Luggage = 3,
                        Year = 2024,
                        Available = true,
                        Featured = true,
                        Mileage = "Unlimited km",
                        Image = "tesla_model3.jpg",
                        Description = "Dual motor all-wheel drive, 570km range, full glass roof, and premium audio system. Perfect for serene New Zealand touring."
                    },
                    new CarDescription
                    {
                        Brand = "Toyota",
                        Model = "RAV4 Hybrid AWD",
                        CarTypeId = suvCat.Id,
                        CarTypeName = suvCat.Name,
                        Color = "Silver Sky",
                        RatePerDay = 89.00m,
                        Transmission = "Automatic",
                        FuelType = "Hybrid",
                        Seats = 5,
                        Luggage = 4,
                        Year = 2024,
                        Available = true,
                        Featured = true,
                        Mileage = "Unlimited km",
                        Image = "toyota_rav4.jpg",
                        Description = "New Zealand's favorite touring SUV. Exceptional fuel economy with electronic all-wheel drive stability on alpine passes."
                    },
                    new CarDescription
                    {
                        Brand = "BMW",
                        Model = "330i M Sport",
                        CarTypeId = sedanCat.Id,
                        CarTypeName = sedanCat.Name,
                        Color = "Portimao Blue",
                        RatePerDay = 139.00m,
                        Transmission = "Automatic",
                        FuelType = "Petrol",
                        Seats = 5,
                        Luggage = 3,
                        Year = 2023,
                        Available = true,
                        Featured = true,
                        Mileage = "Unlimited km",
                        Image = "bmw_330i.jpg",
                        Description = "The definitive sports saloon. Dynamic handling, heated leather sports seats, Head-Up Display, and wireless Apple CarPlay."
                    },
                    new CarDescription
                    {
                        Brand = "Audi",
                        Model = "Q7 50 TDI quattro",
                        CarTypeId = luxCat.Id,
                        CarTypeName = luxCat.Name,
                        Color = "Mythos Black",
                        RatePerDay = 195.00m,
                        Transmission = "Automatic",
                        FuelType = "Diesel",
                        Seats = 7,
                        Luggage = 5,
                        Year = 2023,
                        Available = true,
                        Featured = true,
                        Mileage = "Unlimited km",
                        Image = "audi_q7.jpg",
                        Description = "Peerless 7-seat luxury with adaptive air suspension, matrix LED headlights, panoramic sunroof, and quattro confidence."
                    },
                    new CarDescription
                    {
                        Brand = "Hyundai",
                        Model = "Staria 8-Seater",
                        CarTypeId = vanCat.Id,
                        CarTypeName = vanCat.Name,
                        Color = "Graphite Gray",
                        RatePerDay = 125.00m,
                        Transmission = "Automatic",
                        FuelType = "Diesel",
                        Seats = 8,
                        Luggage = 6,
                        Year = 2024,
                        Available = true,
                        Featured = false,
                        Mileage = "Unlimited km",
                        Image = "hyundai_staria.jpg",
                        Description = "Futuristic family and group transport. Generous legroom across all three rows with power sliding doors and smart cruise control."
                    },
                    new CarDescription
                    {
                        Brand = "Mazda",
                        Model = "CX-5 Takami AWD",
                        CarTypeId = suvCat.Id,
                        CarTypeName = suvCat.Name,
                        Color = "Soul Red Crystal",
                        RatePerDay = 95.00m,
                        Transmission = "Automatic",
                        FuelType = "Petrol",
                        Seats = 5,
                        Luggage = 3,
                        Year = 2023,
                        Available = true,
                        Featured = false,
                        Mileage = "Unlimited km",
                        Image = "mazda_cx5.jpg",
                        Description = "Nappa leather upholstery, real Sen wood trim, 360-degree monitor, and responsive turbocharged AWD performance."
                    }
                };

                await projectContext.CarDescriptions.AddRangeAsync(fleet);
                await projectContext.SaveChangesAsync();
            }

            // 7. Seed Initial Sample Bookings if none exist
            if (!await projectContext.Bookings.AnyAsync())
            {
                var vehicle = await projectContext.CarDescriptions.FirstOrDefaultAsync();
                var pickup = await projectContext.Locations.FirstOrDefaultAsync();
                var ret = await projectContext.Locations.Skip(1).FirstOrDefaultAsync() ?? pickup;

                if (vehicle != null && pickup != null && ret != null)
                {
                    var sampleBooking = new Booking
                    {
                        BookingReference = "NZR-2026-DEMO1",
                        UserId = existingCustomer?.Id ?? "demo-user",
                        CustomerName = "Emma Walker",
                        CustomerEmail = "customer@example.co.nz",
                        CustomerPhone = "+64 21 555 7890",
                        DriverLicenseNumber = "NZL-774412",
                        CarDescriptionId = vehicle.CarDescriptionId,
                        PickupLocationId = pickup.LocationId,
                        ReturnLocationId = ret.LocationId,
                        PickupDateTime = DateTime.UtcNow.AddDays(7),
                        ReturnDateTime = DateTime.UtcNow.AddDays(12),
                        RentalDays = 5,
                        DailyRate = vehicle.RatePerDay,
                        DurationDiscountPercent = 0,
                        DiscountAmount = 0,
                        ExtrasTotal = 50.00m,
                        SubTotal = (vehicle.RatePerDay * 5) + 50.00m,
                        TaxAmount = Math.Round(((vehicle.RatePerDay * 5) + 50.00m) * 0.15m, 2),
                        TotalAmount = Math.Round(((vehicle.RatePerDay * 5) + 50.00m) * 1.15m, 2),
                        SecurityDeposit = 250.00m,
                        Status = BookingStatus.Confirmed,
                        CustomerNotes = "Arriving on flight NZ402 from Wellington.",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    };

                    sampleBooking.BookingExtras.Add(new BookingExtra
                    {
                        Name = "GPS Navigation System",
                        DailyPrice = 10.00m,
                        Quantity = 1,
                        TotalPrice = 50.00m
                    });

                    sampleBooking.Payment = new Payment
                    {
                        TransactionReference = "TXN-INIT-001928",
                        PaymentMethod = "Pay at Pickup",
                        Amount = sampleBooking.TotalAmount,
                        Currency = "NZD",
                        Status = "Pending",
                        ProcessedAt = DateTime.UtcNow.AddDays(-1)
                    };

                    projectContext.Bookings.Add(sampleBooking);
                    await projectContext.SaveChangesAsync();
                }
            }
        }

        private static async Task ExecuteScriptSafelyAsync(DatabaseFacade database, string script)
        {
            var isSqlite = database.IsSqlite();
            if (isSqlite)
            {
                script = script.Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ");
                script = script.Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ");
                script = script.Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ");
            }

            var statements = script.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var stmt in statements)
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                try
                {
                    await database.ExecuteSqlRawAsync(trimmed + ";");
                }
                catch
                {
                    // Ignore errors if object already exists
                }
            }
        }
    }
}
