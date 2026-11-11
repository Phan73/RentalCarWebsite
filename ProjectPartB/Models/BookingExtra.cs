using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPartB.Models
{
    public class BookingExtra
    {
        public int BookingExtraId { get; set; }

        public int BookingId { get; set; }
        public virtual Booking? Booking { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DailyPrice { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalPrice { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
