using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Models;

namespace ProjectPartB.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UserRolesController : Controller
    {
        private readonly UserManager<WebUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<WebUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: UserRoles
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        // GET: UserRoles/CreateRole
        public IActionResult CreateRole()
        {
            return View();
        }

        // POST: UserRoles/CreateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var roleExists = await _roleManager.RoleExistsAsync(model.RoleName.Trim());
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.RoleName.Trim()));
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: UserRoles/EditRole/5
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var model = new EditRoleViewModel { RoleId = role.Id, RoleName = role.Name ?? "" };
            return View(model);
        }

        // POST: UserRoles/EditRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(model.RoleId);
                if (role == null) return NotFound();

                role.Name = model.RoleName.Trim();
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        // POST: UserRoles/DeleteRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UserRoles/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<AssignRoleViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var userWithRoles = new AssignRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? "Unknown",
                    RoleName = string.Join(", ", userRoles),
                    Roles = await _roleManager.Roles.ToListAsync()
                };

                model.Add(userWithRoles);
            }

            return View(model);
        }

        // GET: UserRoles/AssignRole
        public async Task<IActionResult> AssignRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new AssignRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                RoleName = userRoles.FirstOrDefault(),
                Roles = await _roleManager.Roles.ToListAsync()
            };

            return View(model);
        }

        // POST: UserRoles/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(AssignRoleViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
            {
                ModelState.AddModelError("", "Role not found.");
                model.Roles = await _roleManager.Roles.ToListAsync();
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await _userManager.AddToRoleAsync(user, role.Name!);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }
    }
}
