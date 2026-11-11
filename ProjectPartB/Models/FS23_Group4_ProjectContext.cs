using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectPartB.Areas.Identity.Data;

namespace ProjectPartB.Models
{
    public partial class FS23_Group4_ProjectContext : DbContext
    {
        public FS23_Group4_ProjectContext()
        {
        }

        public FS23_Group4_ProjectContext(DbContextOptions<FS23_Group4_ProjectContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CarAvailability> CarAvailabilities { get; set; } = null!;
        public virtual DbSet<CarDescription> CarDescriptions { get; set; } = null!;
        public virtual DbSet<CarType> CarTypes { get; set; } = null!;
        public virtual DbSet<Location> Locations { get; set; } = null!;
        public virtual DbSet<Booking> Bookings { get; set; } = null!;
        public virtual DbSet<BookingExtra> BookingExtras { get; set; } = null!;
        public virtual DbSet<Payment> Payments { get; set; } = null!;
        public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; } = null!;
        public virtual DbSet<UserDescription> UserDescriptions { get; set; } = null!;
        public virtual DbSet<UserEnum> UserEnums { get; set; } = null!;
        public virtual DbSet<CartItem> CartItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CarAvailability>(entity =>
            {
                entity.ToTable("CarAvailability");

                entity.HasOne(d => d.CarDescription)
                    .WithMany(p => p.CarAvailabilities)
                    .HasForeignKey(d => d.CarDescriptionId)
                    .HasConstraintName("FK__CarAvaila__CarDe__395884C4");
            });

            modelBuilder.Entity<CarDescription>(entity =>
            {
                entity.ToTable("CarDescription");

                entity.Property(e => e.Available).HasDefaultValue(true);
                entity.Property(e => e.Brand).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Model).HasMaxLength(50);
                entity.Property(e => e.RatePerDay).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Transmission).HasMaxLength(20).HasDefaultValue("Automatic");
                entity.Property(e => e.FuelType).HasMaxLength(20).HasDefaultValue("Petrol");
                entity.Property(e => e.Seats).HasDefaultValue(5);
                entity.Property(e => e.Luggage).HasDefaultValue(2);
                entity.Property(e => e.Year).HasDefaultValue(2023);
                entity.Property(e => e.Featured).HasDefaultValue(false);
                entity.Property(e => e.Mileage).HasMaxLength(50).HasDefaultValue("Unlimited km");

                entity.HasOne(d => d.CarType)
                    .WithMany(p => p.CarDescriptions)
                    .HasForeignKey(d => d.CarTypeId)
                    .HasConstraintName("FK__CarDescri__CarTy__2BFE89A6");
            });

            modelBuilder.Entity<CarType>(entity =>
            {
                entity.ToTable("CarType");
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Location");
                entity.HasKey(e => e.LocationId);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Booking");
                entity.HasKey(e => e.BookingId);
                entity.HasIndex(e => e.BookingReference).IsUnique();
                entity.Property(e => e.BookingReference).HasMaxLength(50);
                entity.Property(e => e.CustomerName).HasMaxLength(100);
                entity.Property(e => e.CustomerEmail).HasMaxLength(100);
                entity.Property(e => e.CustomerPhone).HasMaxLength(30);
                entity.Property(e => e.DriverLicenseNumber).HasMaxLength(50);
                entity.Property(e => e.DailyRate).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.DurationDiscountPercent).HasColumnType("decimal(5, 2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.ExtrasTotal).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.SubTotal).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

                entity.HasOne(b => b.CarDescription)
                    .WithMany(c => c.Bookings)
                    .HasForeignKey(b => b.CarDescriptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.PickupLocation)
                    .WithMany(l => l.PickupBookings)
                    .HasForeignKey(b => b.PickupLocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.ReturnLocation)
                    .WithMany(l => l.ReturnBookings)
                    .HasForeignKey(b => b.ReturnLocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(b => b.BookingExtras)
                    .WithOne(e => e.Booking)
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Payment)
                    .WithOne(p => p.Booking)
                    .HasForeignKey<Payment>(p => p.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BookingExtra>(entity =>
            {
                entity.ToTable("BookingExtra");
                entity.HasKey(e => e.BookingExtraId);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.DailyPrice).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payment");
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.TransactionReference).HasMaxLength(100);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.Currency).HasMaxLength(10);
                entity.Property(e => e.Status).HasMaxLength(30);
                entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            });

            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.ToTable("MaintenanceRecord");
                entity.HasKey(e => e.MaintenanceRecordId);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Cost).HasColumnType("decimal(10, 2)");

                entity.HasOne(m => m.CarDescription)
                    .WithMany(c => c.MaintenanceRecords)
                    .HasForeignKey(m => m.CarDescriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserDescription>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PK__UserDesc__1788CC4C44B75D43");
                entity.ToTable("UserDescription");
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(15);
            });

            modelBuilder.Entity<UserEnum>(entity =>
            {
                entity.ToTable("UserEnum");
                entity.Property(e => e.UserEnumId).ValueGeneratedNever();
                entity.Property(e => e.Description).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
