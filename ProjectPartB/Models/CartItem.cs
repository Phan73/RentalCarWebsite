using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPartB.Models
{
    public class CartItem
    {
        [Key]
        public string AspNetUserId { get; set; } = string.Empty;
        public int CarDescriptionId { get; set; }
        public virtual CarDescription? Car { get; set; }
      
        public int Quantity { get; set; }
        public decimal RentalDuration { get; set; }
        public DateTime StartDate { get; set; }
    }
}
