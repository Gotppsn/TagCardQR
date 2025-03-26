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
            RawJsonData = @"{""Detail_TH_FirstName"":""แอดมิน"",""Detail_TH_LastName"":""ทดสอบ""}"
        };
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
                                    userInfo.UserCode = GetPropertyValue(dirEntry, "User_Code");
                                    
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
                                        var userCodeMatch = System.Text.RegularExpressions.Regex.Match(
                                            userInfo.RawJsonData, "\"User_Code\":\"(\\d+)\"");
                                        
                                        if (userCodeMatch.Success && userCodeMatch.Groups.Count > 1)
                                        {
                                            userInfo.UserCode = userCodeMatch.Groups[1].Value;
                                        }
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
}
}