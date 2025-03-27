// Path: Services/UserProfileService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CardTagManager.Services
{
    public class UserProfileService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(ApplicationDbContext dbContext, ILogger<UserProfileService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserProfile> GetUserProfileAsync(string username)
        {
            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        
        public async Task<UserProfile> GetUserProfileByIdAsync(int id)
        {
            return await _dbContext.UserProfiles.FindAsync(id);
        }
        
        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            return await _dbContext.UserProfiles
                .OrderBy(u => u.Username)
                .ToListAsync();
        }
        
        public async Task<UserProfile> CreateUserProfileIfNotExistsAsync(
            string username, 
            string firstName = "", 
            string lastName = "", 
            string email = "", 
            string department = "", 
            string plant = "", 
            string userCode = "",
            string thaiFirstName = "",
            string thaiLastName = "",
            string rawJsonData = "")
        {
            try
            {
                _logger.LogInformation($"Creating/updating profile for {username}");
                _logger.LogInformation($"TH: {thaiFirstName} {thaiLastName}, Plant: {plant}, UserCode: {userCode}");
                
                var existingProfile = await GetUserProfileAsync(username);
                
                if (existingProfile != null)
                {
                    // Update with new data - direct assignment for critical fields
                    existingProfile.Detail_EN_FirstName = !string.IsNullOrWhiteSpace(firstName) ? 
                        firstName : existingProfile.Detail_EN_FirstName;
                        
                    existingProfile.Detail_EN_LastName = !string.IsNullOrWhiteSpace(lastName) ? 
                        lastName : existingProfile.Detail_EN_LastName;
                        
                    existingProfile.Detail_TH_FirstName = !string.IsNullOrWhiteSpace(thaiFirstName) ? 
                        thaiFirstName : existingProfile.Detail_TH_FirstName;
                        
                    existingProfile.Detail_TH_LastName = !string.IsNullOrWhiteSpace(thaiLastName) ? 
                        thaiLastName : existingProfile.Detail_TH_LastName;
                        
                    existingProfile.User_Email = !string.IsNullOrWhiteSpace(email) ? 
                        email : existingProfile.User_Email;
                        
                    existingProfile.Department_Name = !string.IsNullOrWhiteSpace(department) ? 
                        department : existingProfile.Department_Name;
                        
                    existingProfile.Plant_Name = !string.IsNullOrWhiteSpace(plant) ? 
                        plant : existingProfile.Plant_Name;
                        
                    existingProfile.User_Code = !string.IsNullOrWhiteSpace(userCode) ? 
                        userCode : existingProfile.User_Code;
                        
                    // Always update login time
                    existingProfile.LastLoginAt = DateTime.Now;
                    
                    _logger.LogInformation("Saving profile with TH names: " +
                        $"{existingProfile.Detail_TH_FirstName} {existingProfile.Detail_TH_LastName}, " +
                        $"Plant: {existingProfile.Plant_Name}, UserCode: {existingProfile.User_Code}");
                    
                    await _dbContext.SaveChangesAsync();
                    
                    return existingProfile;
                }
                
                // Create new profile with all available data
                var newProfile = new UserProfile
                {
                    Username = username,
                    Detail_TH_FirstName = thaiFirstName ?? "",
                    Detail_TH_LastName = thaiLastName ?? "",
                    Detail_EN_FirstName = firstName ?? "",
                    Detail_EN_LastName = lastName ?? "",
                    User_Email = email ?? "",
                    Department_Name = department ?? "",
                    Plant_Name = plant ?? "",
                    User_Code = userCode ?? "",
                    FirstLoginAt = DateTime.Now,
                    LastLoginAt = DateTime.Now,
                    IsActive = true
                };
                
                _dbContext.UserProfiles.Add(newProfile);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Created new profile for {username} with ID: {newProfile.Id}");
                
                return newProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating/updating profile for {username}");
                throw;
            }
        }
        
        public async Task<bool> UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                var existingProfile = await _dbContext.UserProfiles
                    .FirstOrDefaultAsync(u => u.Id == userProfile.Id);
                
                if (existingProfile == null)
                {
                    return false;
                }
                
                existingProfile.Detail_TH_FirstName = userProfile.Detail_TH_FirstName;
                existingProfile.Detail_TH_LastName = userProfile.Detail_TH_LastName;
                existingProfile.Detail_EN_FirstName = userProfile.Detail_EN_FirstName;
                existingProfile.Detail_EN_LastName = userProfile.Detail_EN_LastName;
                existingProfile.User_Email = userProfile.User_Email;
                existingProfile.Plant_Name = userProfile.Plant_Name;
                existingProfile.Department_Name = userProfile.Department_Name;
                existingProfile.IsActive = userProfile.IsActive;
                existingProfile.LastLoginAt = DateTime.Now;
                
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user profile for ID {userProfile.Id}");
                return false;
            }
        }
        
public async Task<bool> EnrichUserProfileFromJsonAsync(string username, string jsonData)
{
    try
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return false;
        }
        
        var userProfile = await GetUserProfileAsync(username);
        if (userProfile == null)
        {
            return false;
        }
        
        bool modified = false;
        
        // Extract Thai first name
        var thFirstNameMatch = Regex.Match(jsonData, "\"Detail_TH_FirstName\"\\s*:\\s*\"([^\"]+)\"");
        if (thFirstNameMatch.Success && thFirstNameMatch.Groups.Count > 1 && 
            string.IsNullOrWhiteSpace(userProfile.Detail_TH_FirstName))
        {
            userProfile.Detail_TH_FirstName = thFirstNameMatch.Groups[1].Value;
            modified = true;
            _logger.LogInformation($"Extracted Thai first name: {userProfile.Detail_TH_FirstName}");
        }
        
        // Extract Thai last name
        var thLastNameMatch = Regex.Match(jsonData, "\"Detail_TH_LastName\"\\s*:\\s*\"([^\"]+)\"");
        if (thLastNameMatch.Success && thLastNameMatch.Groups.Count > 1 && 
            string.IsNullOrWhiteSpace(userProfile.Detail_TH_LastName))
        {
            userProfile.Detail_TH_LastName = thLastNameMatch.Groups[1].Value;
            modified = true;
            _logger.LogInformation($"Extracted Thai last name: {userProfile.Detail_TH_LastName}");
        }
        
        // Extract User_Code if not already set
        var userCodeMatch = Regex.Match(jsonData, "\"User_Code\"\\s*:\\s*\"([^\"]+)\"");
        if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1 && 
            string.IsNullOrWhiteSpace(userProfile.User_Code))
        {
            userProfile.User_Code = userCodeMatch.Groups[1].Value;
            modified = true;
            _logger.LogInformation($"Extracted User_Code: {userProfile.User_Code}");
        }
        
        if (modified)
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error enriching user profile from JSON for {username}");
        return false;
    }
}

        // Role-related methods
        private async Task<bool> AssignDefaultRoleAsync(int userId)
        {
            try
            {
                // Get the default User role (ID 3 based on seed data)
                var userRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "USER");
                if (userRole == null)
                {
                    _logger.LogWarning("Default User role not found when assigning roles to new user");
                    return false;
                }

                // Check if user already has this role
                bool hasRole = await _dbContext.UserRoles
                    .AnyAsync(ur => ur.UserId == userId && ur.RoleId == userRole.Id);
                
                if (hasRole)
                {
                    return true; // Already has the role
                }

                // Assign the default role
                var newUserRole = new UserRole
                {
                    UserId = userId,
                    RoleId = userRole.Id,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                };

                _dbContext.UserRoles.Add(newUserRole);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"Assigned default User role to user ID {userId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning default role to user {userId}");
                return false;
            }
        }
    }
}