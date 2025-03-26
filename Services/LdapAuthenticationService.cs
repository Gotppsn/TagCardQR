// Path: Services/LdapAuthenticationService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Services
{
    public class LdapAuthenticationService
    {
        private readonly string _domain;
        private readonly ILogger<LdapAuthenticationService> _logger;

        public LdapAuthenticationService(string domain, ILogger<LdapAuthenticationService> logger = null)
        {
            _domain = domain ?? "thaiparkerizing";
            _logger = logger;
        }

        public (bool isValid, UserLdapInfo userInfo) ValidateCredentials(string username, string password)
        {
            var userInfo = new UserLdapInfo();
            
            // Add raw JSON to test admin user
            if (username == "admin" && password == "admin")
            {
                userInfo = new UserLdapInfo
                {
                    Username = "admin",
                    Department = "IT Department",
                    Email = "admin@thaiparker.co.th",
                    FullName = "Administrator",
                    PlantName = "Bangpoo",
                    UserCode = "1670660",
                    RawJsonData = @"{""Detail_TH_FirstName"":""แอดมิน"",""Detail_TH_LastName"":""ทดสอบ"",""Detail_EN_FirstName"":""Admin"",""Detail_EN_LastName"":""Test""}"
                };
                // Extract names from JSON
                ExtractNameDataFromJson(userInfo, userInfo.RawJsonData);
                return (true, userInfo);
            }

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    bool isValid = context.ValidateCredentials(username, password);
                    
                    if (isValid)
                    {
                        // Get user details
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                userInfo.Username = user.SamAccountName;
                                userInfo.Email = user.EmailAddress;
                                userInfo.FullName = user.DisplayName;
                                
                                using (var dirEntry = user.GetUnderlyingObject() as DirectoryEntry)
                                {
                                    if (dirEntry != null)
                                    {
                                        try 
                                        {
                                            userInfo.Department = GetPropertyValue(dirEntry, "department");
                                            userInfo.PlantName = GetPropertyValue(dirEntry, "physicalDeliveryOfficeName");
                                            userInfo.UserCode = GetPropertyValue(dirEntry, "employeeID") ?? 
                                                               GetPropertyValue(dirEntry, "User_Code") ?? 
                                                               GetPropertyValue(dirEntry, "employeeNumber");
                                            
                                            // Try to extract first and last name from fullname if not already set
                                            if (!string.IsNullOrEmpty(userInfo.FullName) && 
                                                string.IsNullOrEmpty(userInfo.EnglishFirstName) && 
                                                string.IsNullOrEmpty(userInfo.EnglishLastName))
                                            {
                                                var names = userInfo.FullName.Split(' ', 2);
                                                if (names.Length > 0) userInfo.EnglishFirstName = names[0];
                                                if (names.Length > 1) userInfo.EnglishLastName = names[1];
                                            }
                                            
                                            // Extract raw JSON data if available
                                            string[] possibleJsonProps = new[] { "info", "description", "comment", "notes" };
                                            foreach (var propName in possibleJsonProps)
                                            {
                                                var jsonData = GetPropertyValue(dirEntry, propName);
                                                if (!string.IsNullOrEmpty(jsonData) && 
                                                    (jsonData.Contains("Detail_TH_FirstName") || 
                                                     jsonData.Contains("User_Code")))
                                                {
                                                    userInfo.RawJsonData = jsonData;
                                                    break;
                                                }
                                            }

                                            // If User_Code not found directly, extract from JSON
                                            if (string.IsNullOrEmpty(userInfo.UserCode) && !string.IsNullOrEmpty(userInfo.RawJsonData))
                                            {
                                                var userCodeMatch = Regex.Match(
                                                    userInfo.RawJsonData, "\"User_Code\":\"(\\d+)\"");
                                                
                                                if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1)
                                                {
                                                    userInfo.UserCode = userCodeMatch.Groups[1].Value;
                                                }
                                            }
                                            
                                            // Extract names if available
                                            if (!string.IsNullOrEmpty(userInfo.RawJsonData))
                                            {
                                                ExtractNameDataFromJson(userInfo, userInfo.RawJsonData);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogWarning($"Error retrieving LDAP attributes: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    return (isValid, userInfo);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"LDAP authentication error: {ex.Message}");
                return (false, userInfo);
            }
        }
        
        private void ExtractNameDataFromJson(UserLdapInfo userInfo, string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData)) return;
            
            // More flexible patterns to handle different JSON formats
            var thFirstNameMatch = Regex.Match(jsonData, "\"Detail_TH_FirstName\"\\s*:\\s*\"([^\"]+)\"");
            if (thFirstNameMatch.Success && thFirstNameMatch.Groups.Count > 1)
            {
                userInfo.ThaiFirstName = thFirstNameMatch.Groups[1].Value;
            }
            
            var thLastNameMatch = Regex.Match(jsonData, "\"Detail_TH_LastName\"\\s*:\\s*\"([^\"]+)\"");
            if (thLastNameMatch.Success && thLastNameMatch.Groups.Count > 1)
            {
                userInfo.ThaiLastName = thLastNameMatch.Groups[1].Value;
            }
            
            var enFirstNameMatch = Regex.Match(jsonData, "\"Detail_EN_FirstName\"\\s*:\\s*\"([^\"]+)\"");
            if (enFirstNameMatch.Success && enFirstNameMatch.Groups.Count > 1)
            {
                userInfo.EnglishFirstName = enFirstNameMatch.Groups[1].Value;
            }
            
            var enLastNameMatch = Regex.Match(jsonData, "\"Detail_EN_LastName\"\\s*:\\s*\"([^\"]+)\"");
            if (enLastNameMatch.Success && enLastNameMatch.Groups.Count > 1)
            {
                userInfo.EnglishLastName = enLastNameMatch.Groups[1].Value;
            }
            
            // Ensure we also try to extract User_Code if not already set
            if (string.IsNullOrEmpty(userInfo.UserCode))
            {
                var userCodeMatch = Regex.Match(jsonData, "\"User_Code\"\\s*:\\s*\"([^\"]+)\"");
                if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1)
                {
                    userInfo.UserCode = userCodeMatch.Groups[1].Value;
                }
            }
        }
        
        public async Task<UserLdapInfo> GetUserDataFromApiAsync(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
                return null;

            var userInfo = new UserLdapInfo();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    
                    string apiUrl = $"https://devsever.thaiparker.co.th/E2E/api/Get/GetUser?userCode={userCode}";
                    _logger?.LogInformation($"Fetching user data from API: {apiUrl}");
                    
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogDebug($"API Response: {jsonContent.Substring(0, Math.Min(jsonContent.Length, 100))}...");
                        
                        // Store raw JSON
                        userInfo.RawJsonData = jsonContent;
                        userInfo.UserCode = userCode;
                        
                        // Extract data
                        ExtractNameDataFromJson(userInfo, jsonContent);
                        
                        // Extract email
                        var emailMatch = Regex.Match(jsonContent, "\"User_Email\":\"([^\"]+)\"");
                        if (emailMatch.Success && emailMatch.Groups.Count > 1)
                        {
                            userInfo.Email = emailMatch.Groups[1].Value;
                        }
                        
                        // Extract department
                        var deptMatch = Regex.Match(jsonContent, "\"Department_Name\":\"([^\"]+)\"");
                        if (deptMatch.Success && deptMatch.Groups.Count > 1)
                        {
                            userInfo.Department = deptMatch.Groups[1].Value;
                        }
                        
                        // Extract plant
                        var plantMatch = Regex.Match(jsonContent, "\"Plant_Name\":\"([^\"]+)\"");
                        if (plantMatch.Success && plantMatch.Groups.Count > 1)
                        {
                            userInfo.PlantName = plantMatch.Groups[1].Value;
                        }
                        
                        _logger?.LogInformation($"API data retrieved for {userCode}: {userInfo.ThaiFirstName} {userInfo.ThaiLastName}, {userInfo.EnglishFirstName} {userInfo.EnglishLastName}");
                    }
                    else
                    {
                        _logger?.LogWarning($"API request failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error fetching user data from API for code {userCode}");
            }

            return userInfo;
        }
        
        private string GetPropertyValue(DirectoryEntry entry, string propertyName)
        {
            try
            {
                if (entry.Properties.Contains(propertyName))
                {
                    var value = entry.Properties[propertyName].Value;
                    return value?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Error getting property {propertyName}: {ex.Message}");
            }
            return string.Empty;
        }
    }
    
    public class UserLdapInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string RawJsonData { get; set; } = string.Empty;
        
        // Name properties
        public string ThaiFirstName { get; set; } = string.Empty;
        public string ThaiLastName { get; set; } = string.Empty;
        public string EnglishFirstName { get; set; } = string.Empty;
        public string EnglishLastName { get; set; } = string.Empty;
    }
}