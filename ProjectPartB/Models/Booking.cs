using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectPartB.Areas.Identity.Data;

namespace ProjectPartB.Models
{
    public class Booking
    {
        public Booking()
        {
            BookingExtras = new HashSet<BookingExtra>();
        }

        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingReference { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(30)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DriverLicenseNumber { get; set; } = string.Empty;

        public int CarDescriptionId { get; set; }
        public virtual CarDescription? CarDescription { get; set; }

        public int PickupLocationId { get; set; }
        public virtual Location? PickupLocation { get; set; }

        public int ReturnLocationId { get; set; }
        public virtual Location? ReturnLocation { get; set; }

        public DateTime PickupDateTime { get; set; }
        public DateTime ReturnDateTime { get; set; }

        public int RentalDays { get; set; } = 1;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DailyRate { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal DurationDiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal ExtrasTotal { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal SubTotal { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TaxAmount { get; set; } = 0; // NZ GST 15%

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal SecurityDeposit { get; set; } = 250.00m;

        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

        [StringLength(1000)]
        public string? CustomerNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }

        public virtual ICollection<BookingExtra> BookingExtras { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}
