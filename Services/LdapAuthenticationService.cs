// Path: Services/LdapAuthenticationService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            
            // Support test admin user - ENSURE USERCODE IS POPULATED HERE
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
                _logger?.LogInformation($"Test admin login - setting UserCode to {userInfo.UserCode}");
                return (true, userInfo);
            }

            try
            {
                // For actual LDAP authentication
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    bool isValid = false;
                    
                    // If password is null, we're just doing a lookup without authentication
                    if (password != null)
                    {
                        isValid = context.ValidateCredentials(username, password);
                    }
                    else
                    {
                        isValid = true; // Skip validation when doing lookup only
                    }
                    
                    if (isValid || password == null)
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
                                                
                                                // FIRST APPROACH: Try to get User_Code directly as property
                                                userInfo.UserCode = GetPropertyValue(dirEntry, "User_Code");
                                                _logger?.LogInformation($"First attempt - User_Code={userInfo.UserCode}");
                                                
                                                // SECOND APPROACH: If first approach failed, try extension attribute
                                                if (string.IsNullOrEmpty(userInfo.UserCode))
                                                {
                                                    for (int i = 1; i <= 15; i++)
                                                    {
                                                        string extAttrName = $"extensionAttribute{i}";
                                                        string value = GetPropertyValue(dirEntry, extAttrName);
                                                        
                                                        // Check if this attribute contains User_Code
                                                        if (!string.IsNullOrEmpty(value) && 
                                                            (value.Contains("User_Code") || 
                                                             Regex.IsMatch(value, @"^\d{5,10}$"))) // If it's just a numeric ID
                                                        {
                                                            userInfo.UserCode = Regex.Match(value, @"\d+").Value;
                                                            _logger?.LogInformation($"Found User_Code in {extAttrName}: {userInfo.UserCode}");
                                                            break;
                                                        }
                                                    }
                                                }
                                                
                                                // THIRD APPROACH: If User_Code is still empty and we have access to JSON data
                                                if (string.IsNullOrEmpty(userInfo.UserCode))
                                                {
                                                    // Check multiple possible property names
                                                    string[] possibleJsonProps = new[] { "info", "description", "comment", "notes" };
                                                    
                                                    foreach (var propName in possibleJsonProps)
                                                    {
                                                        var jsonData = GetPropertyValue(dirEntry, propName);
                                                        if (!string.IsNullOrEmpty(jsonData))
                                                        {
                                                            try
                                                            {
                                                                // Try to extract from the JSON structure shown in document #12
                                                                var userCodeMatch = Regex.Match(
                                                                    jsonData, "\"User_Code\":\"(\\d+)\"");
                                                                
                                                                if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1)
                                                                {
                                                                    userInfo.UserCode = userCodeMatch.Groups[1].Value;
                                                                    _logger?.LogInformation($"Extracted User_Code from JSON in {propName}: {userInfo.UserCode}");
                                                                    break;
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                _logger?.LogWarning($"Failed to parse JSON for User_Code in {propName}: {ex.Message}");
                                                            }
                                                        }
                                                    }
                                                }
                                                
                                                // FALLBACK: If still no User_Code, use a default based on username
                                                if (string.IsNullOrEmpty(userInfo.UserCode))
                                                {
                                                    // Try to extract numbers from username if it contains any
                                                    var match = Regex.Match(username, @"\d+");
                                                    if (match.Success)
                                                    {
                                                        userInfo.UserCode = match.Value;
                                                        _logger?.LogInformation($"Using numbers from username as User_Code: {userInfo.UserCode}");
                                                    }
                                                    else
                                                    {
                                                        // Generate a hash code from username
                                                        userInfo.UserCode = Math.Abs(username.GetHashCode()).ToString();
                                                        _logger?.LogInformation($"Generated User_Code from username hash: {userInfo.UserCode}");
                                                    }
                                                }
                                                
                                                _logger?.LogInformation($"Final User_Code for {username}: {userInfo.UserCode}");
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
                    
                    return (isValid || password == null, userInfo);
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