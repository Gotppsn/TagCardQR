// Path: Services/UserProfileService.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        /// <summary>
        /// Gets a user profile by username
        /// </summary>
        public async Task<UserProfile> GetUserProfileAsync(string username)
        {
            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        
        /// <summary>
        /// Updates a user profile with new information
        /// </summary>
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
                
                // Update properties
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
        
        /// <summary>
        /// Creates a complete user profile by extracting information from JSON data
        /// </summary>
        public async Task<bool> EnrichUserProfileFromJsonAsync(string username, string jsonData)
        {
            try
            {
                var userProfile = await GetUserProfileAsync(username);
                if (userProfile == null)
                {
                    return false;
                }
                
                // Try to extract Thai/English names from the JSON if they're empty
                if (string.IsNullOrEmpty(userProfile.Detail_TH_FirstName) || 
                    string.IsNullOrEmpty(userProfile.Detail_TH_LastName) ||
                    string.IsNullOrEmpty(userProfile.Detail_EN_FirstName) ||
                    string.IsNullOrEmpty(userProfile.Detail_EN_LastName))
                {
                    // Sample field extraction - update these based on actual JSON structure
                    // Extract detail_th_firstname
                    var thFirstNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_TH_FirstName\":\"([^\"]+)\"");
                    if (thFirstNameMatch.Success && thFirstNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_TH_FirstName = thFirstNameMatch.Groups[1].Value;
                    }
                    
                    // Extract detail_th_lastname
                    var thLastNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_TH_LastName\":\"([^\"]+)\"");
                    if (thLastNameMatch.Success && thLastNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_TH_LastName = thLastNameMatch.Groups[1].Value;
                    }
                    
                    // Extract detail_en_firstname
                    var enFirstNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_EN_FirstName\":\"([^\"]+)\"");
                    if (enFirstNameMatch.Success && enFirstNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_EN_FirstName = enFirstNameMatch.Groups[1].Value;
                    }
                    
                    // Extract detail_en_lastname
                    var enLastNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_EN_LastName\":\"([^\"]+)\"");
                    if (enLastNameMatch.Success && enLastNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_EN_LastName = enLastNameMatch.Groups[1].Value;
                    }
                    
                    await _dbContext.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enriching user profile from JSON for {username}");
                return false;
            }
        }
    }
}