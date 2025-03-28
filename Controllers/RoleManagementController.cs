// Path: Controllers/RoleManagementController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleManagementController : Controller
    {
        private readonly RoleService _roleService;
        private readonly UserProfileService _userProfileService;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(
            RoleService roleService,
            UserProfileService userProfileService,
            ILogger<RoleManagementController> logger)
        {
            _roleService = roleService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        // GET: RoleManagement
        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return View(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                TempData["ErrorMessage"] = "An error occurred while retrieving roles.";
                return View(Array.Empty<Role>());
            }
        }

        // GET: RoleManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Get users with this role for the detail view
            var usersInRole = await _roleService.GetUsersInRoleAsync(id);
            ViewBag.UsersInRole = usersInRole;

            return View(role);
        }

        // GET: RoleManagement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoleManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _roleService.CreateRoleAsync(role);
                    TempData["SuccessMessage"] = $"Role '{role.Name}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating role");
                    ModelState.AddModelError("", "An error occurred while creating the role.");
                }
            }

            return View(role);
        }

        // GET: RoleManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: RoleManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            if (id != role.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _roleService.UpdateRoleAsync(role);
                    if (result)
                    {
                        TempData["SuccessMessage"] = $"Role '{role.Name}' updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Role not found or could not be updated.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating role");
                    ModelState.AddModelError("", "An error occurred while updating the role.");
                }
            }

            return View(role);
        }

        // GET: RoleManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Get users with this role to show on the delete confirmation page
            var usersInRole = await _roleService.GetUsersInRoleAsync(id);
            ViewBag.UsersInRole = usersInRole;
            ViewBag.CanDelete = !usersInRole.Any();

            return View(role);
        }

        // POST: RoleManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Check if role is assigned to any user
                var usersInRole = await _roleService.GetUsersInRoleAsync(id);
                if (usersInRole.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete role as it is assigned to users. Remove the role from all users first.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                var result = await _roleService.DeleteRoleAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Role deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Role not found or could not be deleted.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting role {id}");
                TempData["ErrorMessage"] = "An error occurred while deleting the role.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: RoleManagement/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userProfileService.GetAllUserProfilesAsync();
                var roles = await _roleService.GetAllRolesAsync();

                ViewBag.Roles = roles;

                // For each user, get their roles
                foreach (var user in users)
                {
                    ViewData[$"UserRoles_{user.Id}"] = await _roleService.GetUserRolesAsync(user.Id);
                }

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for role management");
                TempData["ErrorMessage"] = "An error occurred while retrieving users.";
                return View(Array.Empty<UserProfile>());
            }
        }

        // GET: RoleManagement/EditUserRoles/5
        public async Task<IActionResult> EditUserRoles(int id)
        {
            var user = await _userProfileService.GetUserProfileByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var allRoles = await _roleService.GetAllRolesAsync();
            var userRoles = await _roleService.GetUserRolesAsync(id);

            ViewBag.AllRoles = allRoles;
            ViewBag.UserRoles = userRoles;

            return View(user);
        }

        // POST: RoleManagement/UpdateUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(int userId, int[] selectedRoles)
        {
            try
            {
                var user = await _userProfileService.GetUserProfileByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                var allRoles = await _roleService.GetAllRolesAsync();
                var userRoles = await _roleService.GetUserRolesAsync(userId);

                // Remove roles that are no longer selected
                foreach (var role in userRoles)
                {
                    if (!selectedRoles.Contains(role.Id))
                    {
                        await _roleService.RemoveRoleFromUserAsync(userId, role.Id);
                    }
                }

                // Add newly selected roles
                foreach (var roleId in selectedRoles)
                {
                    var hasRole = userRoles.Any(r => r.Id == roleId);
                    if (!hasRole)
                    {
                        await _roleService.AssignRoleToUserAsync(userId, roleId, User.Identity.Name);
                    }
                }

                TempData["SuccessMessage"] = $"Roles for user '{user.Username}' updated successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating roles for user {userId}");
                TempData["ErrorMessage"] = "An error occurred while updating user roles.";
                return RedirectToAction(nameof(EditUserRoles), new { id = userId });
            }
        }

        // POST: RoleManagement/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            try
            {
                var result = await _roleService.AssignRoleToUserAsync(userId, roleId, User.Identity.Name);
                if (result)
                {
                    var user = await _userProfileService.GetUserProfileByIdAsync(userId);
                    var role = await _roleService.GetRoleByIdAsync(roleId);

                    TempData["SuccessMessage"] = $"Role '{role.Name}' assigned to '{user.Username}' successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign role.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning role {roleId} to user {userId}");
                TempData["ErrorMessage"] = "An error occurred while assigning the role.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: RoleManagement/RemoveRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(int userId, int roleId)
        {
            try
            {
                var result = await _roleService.RemoveRoleFromUserAsync(userId, roleId);
                if (result)
                {
                    var user = await _userProfileService.GetUserProfileByIdAsync(userId);
                    var role = await _roleService.GetRoleByIdAsync(roleId);

                    TempData["SuccessMessage"] = $"Role '{role.Name}' removed from '{user.Username}' successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to remove role.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing role {roleId} from user {userId}");
                TempData["ErrorMessage"] = "An error occurred while removing the role.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}