// Path: Services/LdapAuthenticationService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            
            // Support test admin user
            if (username == "admin" && password == "admin")
            {
                userInfo = new UserLdapInfo
                {
                    Username = "admin",
                    Department = "IT Department",
                    Email = "admin@thaiparker.co.th",
                    FullName = "Administrator",
                    PlantName = "Bangpoo",
                    UserCode = "1670660" // Example User_Code for testing
                };
                return (true, userInfo);
            }

            try
            {
                // For actual LDAP authentication
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    bool isValid = context.ValidateCredentials(username, password);
                    
                    if (isValid)
                    {
                        try 
                        {
                            // Get user information from AD
                            using (var user = UserPrincipal.FindByIdentity(context, username))
                            {
                                if (user != null)
                                {
                                    userInfo.Username = user.SamAccountName;
                                    userInfo.Email = user.EmailAddress;
                                    userInfo.FullName = user.DisplayName;
                                    
                                    // Extract properties from directory entry
                                    using (var dirEntry = user.GetUnderlyingObject() as DirectoryEntry)
                                    {
                                        if (dirEntry != null)
                                        {
                                            try 
                                            {
                                                userInfo.Department = GetPropertyValue(dirEntry, "department");
                                                userInfo.PlantName = GetPropertyValue(dirEntry, "physicalDeliveryOfficeName");
                                                
                                                // Try to get User_Code - must be exact property name as in AD
                                                userInfo.UserCode = GetPropertyValue(dirEntry, "User_Code");
                                                
                                                // If User_Code is empty and we have access to JSON data in the example from document #12
                                                if (string.IsNullOrEmpty(userInfo.UserCode))
                                                {
                                                    var jsonData = GetPropertyValue(dirEntry, "info");
                                                    if (!string.IsNullOrEmpty(jsonData))
                                                    {
                                                        try
                                                        {
                                                            // Try to extract from the JSON structure shown in document #12
                                                            // This is a very specific approach based on the provided example
                                                            var userCodeMatch = System.Text.RegularExpressions.Regex.Match(
                                                                jsonData, "\"User_Code\":\"(\\d+)\"");
                                                            
                                                            if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1)
                                                            {
                                                                userInfo.UserCode = userCodeMatch.Groups[1].Value;
                                                                _logger?.LogInformation($"Extracted User_Code from JSON: {userInfo.UserCode}");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger?.LogWarning($"Failed to parse JSON for User_Code: {ex.Message}");
                                                        }
                                                    }
                                                }
                                                
                                                _logger?.LogInformation($"Retrieved User_Code for {username}: {userInfo.UserCode}");
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger?.LogWarning($"Error retrieving LDAP attributes for {username}: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Error retrieving user details: {ex.Message}");
                        }
                    }
                    
                    return (isValid, userInfo);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"LDAP Authentication error: {ex.Message}");
                return (false, userInfo);
            }
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
    }
}