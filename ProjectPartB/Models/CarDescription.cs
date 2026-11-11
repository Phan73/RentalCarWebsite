using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPartB.Models
{
    public partial class CarDescription
    {
        public CarDescription()
        {
            CarAvailabilities = new HashSet<CarAvailability>();
            Bookings = new HashSet<Booking>();
            MaintenanceRecords = new HashSet<MaintenanceRecord>();
        }

        public int CarDescriptionId { get; set; }
        public int? CarTypeId { get; set; }

        [Required(ErrorMessage = "The description is required.")]
        [StringLength(500, ErrorMessage = "The description cannot be longer than 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "The brand is required.")]
        [StringLength(50)]
        public string? Brand { get; set; }

        [Required(ErrorMessage = "The model is required.")]
        [StringLength(50)]
        public string? Model { get; set; }

        [Required(ErrorMessage = "The color is required.")]
        [StringLength(20)]
        public string? Color { get; set; }

        [Required(ErrorMessage = "The rate per day is required.")]
        [Range(0, 10000, ErrorMessage = "The rate per day must be between 0 and 10000.")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal RatePerDay { get; set; }

        public bool? Available { get; set; } = true;

        [Required(ErrorMessage = "Please upload an image.")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Please select the car type.")]
        public string CarTypeName { get; set; } = string.Empty;

        // Modern automotive rental specifications
        [StringLength(20)]
        public string Transmission { get; set; } = "Automatic";

        [StringLength(20)]
        public string FuelType { get; set; } = "Petrol";

        [Range(1, 12)]
        public int Seats { get; set; } = 5;

        [Range(0, 10)]
        public int Luggage { get; set; } = 2;

        [Range(2000, 2030)]
        public int Year { get; set; } = 2023;

        public bool Featured { get; set; } = false;

        [StringLength(50)]
        public string Mileage { get; set; } = "Unlimited km";

        public virtual CarType? CarType { get; set; }
        public virtual ICollection<CarAvailability> CarAvailabilities { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; }
    }
}
