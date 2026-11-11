using System.ComponentModel.DataAnnotations;

namespace ProjectPartB.Models
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
