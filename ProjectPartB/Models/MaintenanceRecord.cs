using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPartB.Models
{
    public class MaintenanceRecord
    {
        public int MaintenanceRecordId { get; set; }

        public int CarDescriptionId { get; set; }
        public virtual CarDescription? CarDescription { get; set; }

        public DateTime MaintenanceDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Cost { get; set; }

        public bool IsResolved { get; set; } = false;
    }
}
