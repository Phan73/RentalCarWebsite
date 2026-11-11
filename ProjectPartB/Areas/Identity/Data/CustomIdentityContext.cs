using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Models;

namespace ProjectPartB.Areas.Identity.Data;

public class CustomIdentityContext : IdentityDbContext<WebUser>
{
    public CustomIdentityContext(DbContextOptions<CustomIdentityContext> options)
        : base(options)
    {
    }
    public DbSet<Membership> Memberships { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Membership One-to-One
        builder.Entity<Membership>()
            .HasOne(m => m.User)
            .WithOne(u => u.Membership)
            .HasForeignKey<Membership>(m => m.UserId);

        // Seed Roles
        var adminRoleId = "a1-role-admin";
        var managerRoleId = "a2-role-manager";
        var staffRoleId = "a3-role-staff";
        var customerRoleId = "a4-role-customer";

        var roles = new List<IdentityRole>
        {
            new IdentityRole { Id = adminRoleId, Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
            new IdentityRole { Id = managerRoleId, Name = "Manager", NormalizedName = "MANAGER" },
            new IdentityRole { Id = staffRoleId, Name = "Staff", NormalizedName = "STAFF" },
            new IdentityRole { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER" }
        };
        builder.Entity<IdentityRole>().HasData(roles);

        // Seed Users
        var passwordHasher = new PasswordHasher<WebUser>();

        var adminId = "u1-admin-user";
        var managerId = "u2-manager-user";

        var admin = new WebUser
        {
            Id = adminId,
            FullName = "Apex Operations Lead",
            Address = "100 Queen Street, Auckland CBD",
            Phone = "+64 9 300 1234",
            DrivingLicense = "DL-NZ-982341",
            UserName = "administrator@example.com",
            NormalizedUserName = "ADMINISTRATOR@EXAMPLE.COM",
            Email = "administrator@example.com",
            NormalizedEmail = "ADMINISTRATOR@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = "f4c935d2-28e7-4f05-9b55-d143c7b2a001",
            ConcurrencyStamp = "a1b2c3d4-e5f6-4a1b-8c2d-3e4f5a6b7c01"
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Password@123");

        var manager = new WebUser
        {
            Id = managerId,
            FullName = "Fleet Manager",
            Address = "Ray Emery Drive, Auckland Airport",
            Phone = "+64 9 275 0789",
            DrivingLicense = "DL-NZ-124982",
            UserName = "manager@example.com",
            NormalizedUserName = "MANAGER@EXAMPLE.COM",
            Email = "manager@example.com",
            NormalizedEmail = "MANAGER@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = "f4c935d2-28e7-4f05-9b55-d143c7b2a002",
            ConcurrencyStamp = "a1b2c3d4-e5f6-4a1b-8c2d-3e4f5a6b7c02"
        };
        manager.PasswordHash = passwordHasher.HashPassword(manager, "Password@123");

        builder.Entity<WebUser>().HasData(admin, manager);

        // Seed UserRoles
        builder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { UserId = adminId, RoleId = adminRoleId },
            new IdentityUserRole<string> { UserId = managerId, RoleId = managerRoleId }
        );
    }
}
