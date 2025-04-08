// Path: Services/RoleService.cs
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
    public class RoleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleService> _logger;

        public RoleService(ApplicationDbContext context, ILogger<RoleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all roles
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        // Get role by ID
        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        // Get role by name
        public async Task<Role> GetRoleByNameAsync(string name)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpper());
        }

        // Create a new role
        public async Task<Role> CreateRoleAsync(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            // Ensure the normalized name is set
            role.NormalizedName = role.Name.ToUpper();
            role.CreatedAt = DateTime.Now;
            role.ConcurrencyStamp = Guid.NewGuid().ToString();

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created new role: {role.Name} (ID: {role.Id})");
            
            return role;
        }

        // Update an existing role
        public async Task<bool> UpdateRoleAsync(Role role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var existingRole = await _context.Roles.FindAsync(role.Id);
            if (existingRole == null)
                return false;

            // Update properties
            existingRole.Name = role.Name;
            existingRole.NormalizedName = role.Name.ToUpper();
            existingRole.Description = role.Description;
            existingRole.UpdatedAt = DateTime.Now;
            existingRole.ConcurrencyStamp = Guid.NewGuid().ToString();

            _context.Roles.Update(existingRole);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated role: {role.Name} (ID: {role.Id})");
            
            return true;
        }

        // Delete a role
        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            // Check if role is in use
            bool isInUse = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (isInUse)
            {
                _logger.LogWarning($"Cannot delete role {role.Name} (ID: {role.Id}) as it is assigned to users");
                return false;
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Deleted role: {role.Name} (ID: {role.Id})");
            
            return true;
        }

        // Assign role to user
        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId, string actionBy = "system")
        {
            // Check if user exists
            var user = await _context.UserProfiles.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Cannot assign role: User with ID {userId} not found");
                return false;
            }

            // Check if role exists
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning($"Cannot assign role: Role with ID {roleId} not found");
                return false;
            }

            // Check if assignment already exists
            bool alreadyAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
                
            if (alreadyAssigned)
            {
                _logger.LogInformation($"Role {role.Name} already assigned to user {user.Username}");
                return true; // Already has the role
            }

            // Create the user role assignment
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                CreatedAt = DateTime.Now,
                CreatedBy = actionBy
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Assigned role {role.Name} to user {user.Username}");
            
            return true;
        }

        // Remove role from user
        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
                
            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed role with ID {roleId} from user with ID {userId}");
            
            return true;
        }

        // Get all roles for a user
        public async Task<List<Role>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r)
                .ToListAsync();
        }

        // Get all users with a specific role
        public async Task<List<UserProfile>> GetUsersInRoleAsync(int roleId)
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Join(_context.UserProfiles,
                    ur => ur.UserId,
                    u => u.Id,
                    (ur, u) => u)
                .ToListAsync();
        }

        // Check if user is in role
        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            return await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { UserRole = ur, Role = r })
                .AnyAsync(x => x.UserRole.UserId == userId && 
                            x.Role.NormalizedName == roleName.ToUpper());
        }
    }
}