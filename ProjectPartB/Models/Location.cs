using System.ComponentModel.DataAnnotations;

namespace ProjectPartB.Models
{
    public class Location
    {
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Location name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Booking> PickupBookings { get; set; } = new HashSet<Booking>();
        public virtual ICollection<Booking> ReturnBookings { get; set; } = new HashSet<Booking>();
    }
}
