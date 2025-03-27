// Path: Controllers/DepartmentAccessController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class DepartmentAccessController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DepartmentAccessService _departmentAccessService;
        private readonly UserProfileService _userProfileService;
        private readonly ILogger<DepartmentAccessController> _logger;

        public DepartmentAccessController(
            ApplicationDbContext context,
            DepartmentAccessService departmentAccessService,
            UserProfileService userProfileService,
            ILogger<DepartmentAccessController> logger)
        {
            _context = context;
            _departmentAccessService = departmentAccessService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        // GET: DepartmentAccess
        public async Task<IActionResult> Index()
        {
            try
            {
                string userDepartment = User.Claims.FirstOrDefault(c => c.Type == "Department")?.Value ?? string.Empty;
                bool isAdmin = User.IsInRole("Admin");
                
                // Admin can see all access, managers can only see their department's access
                if (isAdmin)
                {
                    var allAccesses = await _departmentAccessService.GetAllDepartmentAccessesAsync();
                    return View(allAccesses);
                }
                else
                {
                    var departmentAccesses = await _departmentAccessService.GetDepartmentAccessesByDepartmentAsync(userDepartment);
                    return View(departmentAccesses);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department accesses");
                TempData["ErrorMessage"] = "An error occurred while retrieving department access data.";
                return View(new List<DepartmentAccess>());
            }
        }

        // GET: DepartmentAccess/Manage
        public async Task<IActionResult> Manage()
        {
            try
            {
                // Get current user's department from claims
                string userDepartment = User.Claims.FirstOrDefault(c => c.Type == "Department")?.Value ?? string.Empty;
                bool isAdmin = User.IsInRole("Admin");
                
                // Get all departments - admins can see all, managers only see their own
                var departments = isAdmin 
                    ? await _departmentAccessService.GetAllDepartmentsAsync()
                    : new List<string> { userDepartment };
                
                // Get all users
                var users = await _userProfileService.GetAllUserProfilesAsync();
                
                // For managers, filter out users who are already in their department
                if (!isAdmin)
                {
                    users = users.Where(u => u.Department_Name != userDepartment).ToList();
                }
                
                ViewBag.Departments = departments;
                ViewBag.Users = users;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manage department access page");
                TempData["ErrorMessage"] = "An error occurred while loading the page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DepartmentAccess/Grant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grant(int userId, string departmentName)
        {
            try
            {
                // Get current user's department from claims
                string userDepartment = User.Claims.FirstOrDefault(c => c.Type == "Department")?.Value ?? string.Empty;
                bool isAdmin = User.IsInRole("Admin");
                
                // Verify authorization - admins can grant any access, managers only for their own department
                if (!isAdmin && departmentName != userDepartment)
                {
                    TempData["ErrorMessage"] = "You don't have permission to grant access to this department.";
                    return RedirectToAction(nameof(Manage));
                }
                
                // Get granter information
                string grantedBy = User.Identity?.Name ?? "system";
                string grantedById = User.Claims.FirstOrDefault(c => c.Type == "User_Code")?.Value ?? string.Empty;
                
                // Grant access
                var result = await _departmentAccessService.GrantDepartmentAccessAsync(userId, departmentName, grantedBy, grantedById);
                
                if (result)
                {
                    TempData["SuccessMessage"] = "Department access granted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to grant department access.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error granting department access for user {userId} to department {departmentName}");
                TempData["ErrorMessage"] = "An error occurred while granting department access.";
                return RedirectToAction(nameof(Manage));
            }
        }

        // POST: DepartmentAccess/Revoke/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            try
            {
                _logger.LogInformation($"Attempting to revoke access ID: {id} by user: {User.Identity?.Name}");
                
                // Get the access record to verify it exists and for logging purposes
                var accessToRevoke = await _context.DepartmentAccesses.FindAsync(id);
                if (accessToRevoke == null)
                {
                    TempData["ErrorMessage"] = "Access record not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Log the access details before deletion
                _logger.LogInformation($"Revoking access: ID={id}, UserID={accessToRevoke.UserId}, Department={accessToRevoke.DepartmentName}");
                
                // Remove from context first
                _context.DepartmentAccesses.Remove(accessToRevoke);
                
                try
                {
                    // Save immediately to confirm deletion
                    await _context.SaveChangesAsync();
                    
                    // Also try direct SQL delete as fallback
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM DepartmentAccesses WHERE Id = {0}", id);
                    
                    TempData["SuccessMessage"] = "Department access revoked successfully.";
                    _logger.LogInformation($"Successfully deleted access ID: {id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during DB operation for access ID: {id}");
                    TempData["ErrorMessage"] = $"Database error: {ex.Message}";
                    return RedirectToAction(nameof(Index));
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking department access {id}");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}