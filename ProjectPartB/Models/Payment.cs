using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPartB.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int BookingId { get; set; }
        public virtual Booking? Booking { get; set; }

        [Required]
        [StringLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Pay at Pickup";

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "NZD";

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
