using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ProjectPartB.Models
{
    public class AssignRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public IEnumerable<IdentityRole>? Roles { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string? RoleName { get; set; }
    }
}
