// Path: Services/UserProfileService.cs
using System;
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
                _logger.LogInformation($"Creating/updating profile for {username} with data: " +
                    $"First={firstName}, Last={lastName}, ThFirst={thaiFirstName}, ThLast={thaiLastName}, " +
                    $"Code={userCode}, HasJson={!string.IsNullOrEmpty(rawJsonData)}");
                    
                var existingProfile = await GetUserProfileAsync(username);
                
                if (existingProfile != null)
                {
                    // Always update with new data if it's not empty
                    bool modified = false;
                    
                    if (!string.IsNullOrWhiteSpace(firstName) && existingProfile.Detail_EN_FirstName != firstName)
                    {
                        existingProfile.Detail_EN_FirstName = firstName;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(lastName) && existingProfile.Detail_EN_LastName != lastName)
                    {
                        existingProfile.Detail_EN_LastName = lastName;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(thaiFirstName) && existingProfile.Detail_TH_FirstName != thaiFirstName)
                    {
                        existingProfile.Detail_TH_FirstName = thaiFirstName;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(thaiLastName) && existingProfile.Detail_TH_LastName != thaiLastName)
                    {
                        existingProfile.Detail_TH_LastName = thaiLastName;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(email) && existingProfile.User_Email != email)
                    {
                        existingProfile.User_Email = email;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(department) && existingProfile.Department_Name != department)
                    {
                        existingProfile.Department_Name = department;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(plant) && existingProfile.Plant_Name != plant)
                    {
                        existingProfile.Plant_Name = plant;
                        modified = true;
                    }
                        
                    if (!string.IsNullOrWhiteSpace(userCode) && existingProfile.User_Code != userCode)
                    {
                        existingProfile.User_Code = userCode;
                        modified = true;
                    }
                        
                    // Always update login time
                    existingProfile.LastLoginAt = DateTime.Now;
                    
                    if (modified)
                    {
                        _logger.LogInformation($"Updated profile data for {username}");
                    }
                    
                    await _dbContext.SaveChangesAsync();
                    
                    // If raw JSON data is available, try to extract more data
                    if (!string.IsNullOrEmpty(rawJsonData))
                    {
                        bool jsonEnriched = await EnrichUserProfileFromJsonAsync(username, rawJsonData);
                        if (jsonEnriched)
                        {
                            _logger.LogInformation($"Enriched profile for {username} from JSON data");
                        }
                    }
                    
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
                
                // If raw JSON data is available, try to enrich profile further
                if (!string.IsNullOrEmpty(rawJsonData))
                {
                    await EnrichUserProfileFromJsonAsync(username, rawJsonData);
                }
                
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
                
                // Extract English first name
                var enFirstNameMatch = Regex.Match(jsonData, "\"Detail_EN_FirstName\"\\s*:\\s*\"([^\"]+)\"");
                if (enFirstNameMatch.Success && enFirstNameMatch.Groups.Count > 1 && 
                    string.IsNullOrWhiteSpace(userProfile.Detail_EN_FirstName))
                {
                    userProfile.Detail_EN_FirstName = enFirstNameMatch.Groups[1].Value;
                    modified = true;
                    _logger.LogInformation($"Extracted English first name: {userProfile.Detail_EN_FirstName}");
                }
                
                // Extract English last name
                var enLastNameMatch = Regex.Match(jsonData, "\"Detail_EN_LastName\"\\s*:\\s*\"([^\"]+)\"");
                if (enLastNameMatch.Success && enLastNameMatch.Groups.Count > 1 && 
                    string.IsNullOrWhiteSpace(userProfile.Detail_EN_LastName))
                {
                    userProfile.Detail_EN_LastName = enLastNameMatch.Groups[1].Value;
                    modified = true;
                    _logger.LogInformation($"Extracted English last name: {userProfile.Detail_EN_LastName}");
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
    }
}