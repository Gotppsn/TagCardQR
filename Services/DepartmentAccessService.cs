// Path: Services/DepartmentAccessService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Services
{
    public class DepartmentAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentAccessService> _logger;

        public DepartmentAccessService(ApplicationDbContext context, ILogger<DepartmentAccessService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DepartmentAccess>> GetUserDepartmentAccessesAsync(int userId)
        {
            return await _context.DepartmentAccesses
                .Where(da => da.UserId == userId && da.IsActive)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserAccessibleDepartmentsAsync(int userId)
        {
            var accesses = await _context.DepartmentAccesses
                .Where(da => da.UserId == userId && da.IsActive)
                .Select(da => da.DepartmentName)
                .ToListAsync();
                
            // Also include the user's own department
            var userProfile = await _context.UserProfiles.FindAsync(userId);
            if (userProfile != null && !string.IsNullOrEmpty(userProfile.Department_Name) && 
                !accesses.Contains(userProfile.Department_Name))
            {
                accesses.Add(userProfile.Department_Name);
            }
            
            return accesses;
        }
        
        // Check if user has specific access level to a department
        public async Task<bool> HasAccessLevelToDepartmentAsync(int userId, string departmentName, string accessLevel)
        {
            // User's own department always has full access
            var userProfile = await _context.UserProfiles.FindAsync(userId);
            if (userProfile != null && userProfile.Department_Name == departmentName)
                return true;
                
            // Check for specific access level
            return await _context.DepartmentAccesses
                .AnyAsync(da => da.UserId == userId && 
                          da.DepartmentName == departmentName && 
                          da.IsActive && 
                          (da.AccessLevel == accessLevel || da.AccessLevel == "Edit"));
        }

        public async Task<List<DepartmentAccess>> GetAllDepartmentAccessesAsync()
        {
            return await _context.DepartmentAccesses
                .Include(da => da.User)
                .OrderBy(da => da.DepartmentName)
                .ThenBy(da => da.User.Username)
                .ToListAsync();
        }

        public async Task<List<DepartmentAccess>> GetDepartmentAccessesByDepartmentAsync(string departmentName)
        {
            return await _context.DepartmentAccesses
                .Include(da => da.User)
                .Where(da => da.DepartmentName == departmentName && da.IsActive)
                .OrderBy(da => da.User.Username)
                .ToListAsync();
        }

        public async Task<bool> GrantDepartmentAccessAsync(int userId, string departmentName, string accessLevel, string grantedBy, string grantedById)
        {
            try
            {
                // Normalize department name
                departmentName = departmentName.Trim();
                
                // Validate access level
                if (string.IsNullOrEmpty(accessLevel))
                {
                    accessLevel = "View"; // Default to View if not specified
                }
                
                // Check if access already exists (case-insensitive)
                var existingAccess = await _context.DepartmentAccesses
                    .FirstOrDefaultAsync(da => da.UserId == userId && 
                                        EF.Functions.Collate(da.DepartmentName, "SQL_Latin1_General_CP1_CI_AS") == departmentName);
                
                if (existingAccess != null)
                {
                    // If exists but not active, reactivate it
                    if (!existingAccess.IsActive)
                    {
                        existingAccess.IsActive = true;
                        existingAccess.GrantedAt = DateTime.Now;
                        existingAccess.GrantedBy = grantedBy;
                        existingAccess.GrantedById = grantedById;
                    }
                    
                    // Update access level if it's different
                    if (existingAccess.AccessLevel != accessLevel)
                    {
                        existingAccess.AccessLevel = accessLevel;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated department access for user {userId} to department {departmentName} with access level {accessLevel}");
                    
                    return true;
                }
                
                // Create new access
                var newAccess = new DepartmentAccess
                {
                    UserId = userId,
                    DepartmentName = departmentName,
                    AccessLevel = accessLevel,
                    GrantedAt = DateTime.Now,
                    GrantedBy = grantedBy,
                    GrantedById = grantedById,
                    IsActive = true
                };
                
                _context.DepartmentAccesses.Add(newAccess);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Granted department access for user {userId} to department {departmentName} with access level {accessLevel}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error granting department access for user {userId} to department {departmentName}");
                return false;
            }
        }

        public async Task<bool> RevokeDepartmentAccessAsync(int accessId)
        {
            try
            {
                var access = await _context.DepartmentAccesses.FindAsync(accessId);
                if (access == null)
                    return false;
                
                // Hard delete the record instead of soft delete
                _context.DepartmentAccesses.Remove(access);
                
                // Explicitly call SaveChanges with higher command timeout
                await _context.Database.ExecuteSqlRawAsync(
                    "SET COMMAND TIMEOUT 120; DELETE FROM DepartmentAccesses WHERE Id = @p0", 
                    accessId);
                
                _logger.LogInformation($"Hard deleted department access {accessId} for user {access.UserId} to department {access.DepartmentName}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking department access {accessId}");
                return false;
            }
        }

        public async Task<List<string>> GetAllDepartmentsAsync()
        {
            // Get all unique department names from user profiles
            return await _context.UserProfiles
                .Where(u => !string.IsNullOrEmpty(u.Department_Name))
                .Select(u => u.Department_Name)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        public async Task<bool> HasAccessToDepartmentAsync(int userId, string departmentName)
        {
            // Check if user has their own department matching the requested one
            var userProfile = await _context.UserProfiles.FindAsync(userId);
            if (userProfile != null && userProfile.Department_Name == departmentName)
                return true;
                
            // Check if user has been granted access to the department
            return await _context.DepartmentAccesses
                .AnyAsync(da => da.UserId == userId && da.DepartmentName == departmentName && da.IsActive);
        }
    }
}