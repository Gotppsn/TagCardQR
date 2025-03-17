// Path: Services/LdapAuthenticationService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CardTagManager.Services
{
    public class LdapAuthenticationService
    {
        private readonly string _domain;

        public LdapAuthenticationService(string domain)
        {
            _domain = domain ?? "thaiparkerizing";
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
                    PlantName = "Bangpoo"
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
                        // Get user information from AD
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                userInfo.Username = user.SamAccountName;
                                userInfo.Email = user.EmailAddress;
                                userInfo.FullName = user.DisplayName;
                                
                                // For department and plant, these are typically stored in custom AD attributes
                                // You may need to adjust these based on your AD schema
                                using (var dirEntry = user.GetUnderlyingObject() as System.DirectoryServices.DirectoryEntry)
                                {
                                    if (dirEntry != null)
                                    {
                                        try 
                                        {
                                            userInfo.Department = dirEntry.Properties["department"].Value?.ToString();
                                            // Assuming plant name is stored in a custom attribute like "physicalDeliveryOfficeName"
                                            userInfo.PlantName = dirEntry.Properties["physicalDeliveryOfficeName"].Value?.ToString();
                                        }
                                        catch (Exception ex)
                                        {
                                            // Fallback values if attributes aren't found
                                            userInfo.Department = "Unknown Department";
                                            userInfo.PlantName = "Unknown Plant";
                                            Console.WriteLine($"Error retrieving LDAP attributes: {ex.Message}");
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
                // Log exception silently, production code should use proper logging
                Console.WriteLine($"LDAP Authentication error: {ex.Message}");
                return (false, userInfo);
            }
        }
    }
    
    public class UserLdapInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
    }
}