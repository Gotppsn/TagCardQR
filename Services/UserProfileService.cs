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

        public async Task<UserProfile> GetUserProfileAsync(string username)
        {
            return await _dbContext.UserProfiles
                .FirstOrDefaultAsync(u => u.Username == username);
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
    string thaiLastName = "")
{
    var existingProfile = await GetUserProfileAsync(username);
    
    if (existingProfile != null)
    {
        existingProfile.LastLoginAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return existingProfile;
    }
    
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
    
    return newProfile;
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
                var userProfile = await GetUserProfileAsync(username);
                if (userProfile == null)
                {
                    return false;
                }
                
                if (string.IsNullOrEmpty(userProfile.Detail_TH_FirstName) || 
                    string.IsNullOrEmpty(userProfile.Detail_TH_LastName) ||
                    string.IsNullOrEmpty(userProfile.Detail_EN_FirstName) ||
                    string.IsNullOrEmpty(userProfile.Detail_EN_LastName))
                {
                    var thFirstNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_TH_FirstName\":\"([^\"]+)\"");
                    if (thFirstNameMatch.Success && thFirstNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_TH_FirstName = thFirstNameMatch.Groups[1].Value;
                    }
                    
                    var thLastNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_TH_LastName\":\"([^\"]+)\"");
                    if (thLastNameMatch.Success && thLastNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_TH_LastName = thLastNameMatch.Groups[1].Value;
                    }
                    
                    var enFirstNameMatch = System.Text.RegularExpressions.Regex.Match(
                        jsonData, "\"Detail_EN_FirstName\":\"([^\"]+)\"");
                    if (enFirstNameMatch.Success && enFirstNameMatch.Groups.Count > 1)
                    {
                        userProfile.Detail_EN_FirstName = enFirstNameMatch.Groups[1].Value;
                    }
                    
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