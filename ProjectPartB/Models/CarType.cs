using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectPartB.Models
{
    public partial class CarType
    {
        public CarType()
        {
            CarDescriptions = new HashSet<CarDescription>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<CarDescription> CarDescriptions { get; set; }
    }
}
