using System.ComponentModel.DataAnnotations.Schema;
using ProjectPartB.Areas.Identity.Data;

namespace ProjectPartB.Models
{
    public class Membership
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual WebUser? User { get; set; }
    }
}
