// Path: Services/LdapAuthenticationService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
                                            // Log all available properties
                                            _logger?.LogInformation("All available LDAP attributes for user: " + username);
                                            foreach (PropertyValueCollection property in dirEntry.Properties)
                                            {
                                                string propName = property.PropertyName;
                                                string propValue = "";
                                                
                                                // Handle multi-valued properties
                                                if (property.Count > 0)
                                                {
                                                    if (property.Count == 1)
                                                    {
                                                        propValue = property[0]?.ToString() ?? "(null)";
                                                    }
                                                    else
                                                    {
                                                        // For multi-valued properties, join with commas
                                                        var values = new List<string>();
                                                        for (int i = 0; i < property.Count; i++)
                                                        {
                                                            values.Add(property[i]?.ToString() ?? "(null)");
                                                        }
                                                        propValue = string.Join(", ", values);
                                                    }
                                                }
                                                
                                                _logger?.LogInformation($"LDAP Attribute: {propName} = {propValue}");
                                            }

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
                                            
                                            // Check all properties for potential JSON data
                                            var potentialJsonProps = new[] { 
                                                "info", "description", "comment", "notes", "extensionAttribute1", 
                                                "extensionAttribute2", "extensionAttribute3", "extensionAttribute4",
                                                "extensionAttribute5", "extensionAttribute6", "extensionAttribute7",
                                                "extensionAttribute8", "extensionAttribute9", "extensionAttribute10",
                                                "extensionAttribute11", "extensionAttribute12", "extensionAttribute13",
                                                "extensionAttribute14", "extensionAttribute15", "userParameters" 
                                            };

                                            foreach (var propName in potentialJsonProps)
                                            {
                                                var jsonData = GetPropertyValue(dirEntry, propName);
                                                if (!string.IsNullOrEmpty(jsonData) && 
                                                    (jsonData.Contains("{") || jsonData.Contains("Detail_TH_FirstName") || 
                                                    jsonData.Contains("User_Code")))
                                                {
                                                    _logger?.LogInformation($"Found potential JSON data in {propName}: {jsonData}");
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

        public async Task<Dictionary<string, string>> GetAllLdapAttributesAsync(string username, string password)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    bool isValid = false;
                    if (password != null)
                    {
                        isValid = context.ValidateCredentials(username, password);
                    }
                    
                    // Try to find user even if credentials aren't validated (admin case)
                    using (var user = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (user != null)
                        {
                            // Add basic properties
                            result.Add("SamAccountName", user.SamAccountName ?? "");
                            result.Add("EmailAddress", user.EmailAddress ?? "");
                            result.Add("DisplayName", user.DisplayName ?? "");
                            
                            // Add more UserPrincipal properties
                            result.Add("Description", user.Description ?? "");
                            result.Add("EmployeeId", user.EmployeeId ?? "");
                            result.Add("GivenName", user.GivenName ?? "");
                            result.Add("Surname", user.Surname ?? "");
                            result.Add("MiddleName", user.MiddleName ?? "");
                            result.Add("HomeDirectory", user.HomeDirectory ?? "");
                            result.Add("VoiceTelephoneNumber", user.VoiceTelephoneNumber ?? "");
                            
                            using (var dirEntry = user.GetUnderlyingObject() as DirectoryEntry)
                            {
                                if (dirEntry != null)
                                {
                                    // Add all properties from DirectoryEntry
                                    foreach (PropertyValueCollection property in dirEntry.Properties)
                                    {
                                        string propName = property.PropertyName;
                                        string propValue = "";
                                        
                                        // Handle multi-valued properties
                                        if (property.Count > 0)
                                        {
                                            if (property.Count == 1)
                                            {
                                                propValue = property[0]?.ToString() ?? "(null)";
                                            }
                                            else
                                            {
                                                var values = new List<string>();
                                                for (int i = 0; i < property.Count; i++)
                                                {
                                                    values.Add(property[i]?.ToString() ?? "(null)");
                                                }
                                                propValue = string.Join(", ", values);
                                            }
                                        }
                                        
                                        // Some properties might be binary - skip or convert as needed
                                        if (propValue.Length > 1000 && !propName.ToLower().Contains("json"))
                                        {
                                            propValue = $"[Binary data, length: {propValue.Length} characters]";
                                        }
                                        
                                        result[$"LDAP_{propName}"] = propValue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            result.Add("ERROR", "User not found in directory");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving LDAP attributes: {ex.Message}");
                result.Add("ERROR", ex.Message);
            }
            
            return result;
        }

        public Dictionary<string, object> ParseJsonData(string jsonData)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(jsonData))
                return result;
                
            try
            {
                // Try to parse as pure JSON
                if (jsonData.Trim().StartsWith("{"))
                {
                    // Use System.Text.Json for parsing
                    using (JsonDocument doc = JsonDocument.Parse(jsonData))
                    {
                        JsonElement root = doc.RootElement;
                        foreach (JsonProperty prop in root.EnumerateObject())
                        {
                            result[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
                else
                {
                    // Try to extract json-like key-value pairs with regex
                    var matches = Regex.Matches(jsonData, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            string key = match.Groups[1].Value;
                            string value = match.Groups[2].Value;
                            result[key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Error parsing JSON data: {ex.Message}");
                result["ERROR"] = ex.Message;
                result["RawData"] = jsonData;
            }
            
            return result;
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