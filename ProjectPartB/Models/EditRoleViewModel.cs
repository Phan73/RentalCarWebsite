using System.ComponentModel.DataAnnotations;

namespace ProjectPartB.Models
{
    public class EditRoleViewModel
    {
        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
