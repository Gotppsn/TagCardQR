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
                    UserCode = "0000000",
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
                                
                                // This is the userID from LDAP Description field
                                userInfo.UserCode = user.Description;
                                
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
                                                              GetPropertyValue(dirEntry, "employeeNumber") ??
                                                              user.Description;
                                            
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
                    Console.WriteLine($"[API-REQUEST] Fetching user data from: {apiUrl}");
                    
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    Console.WriteLine($"[API-RESPONSE] Status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[API-DATA] Raw response: {jsonContent}");
                        
                        // Store raw JSON
                        userInfo.RawJsonData = jsonContent;
                        userInfo.UserCode = userCode;
                        
                        try
                        {
                            // Parse with direct string search to debug
                            string[] jsonLines = jsonContent.Split('\n');
                            Console.WriteLine("[API-PARSE] Line-by-line analysis:");
                            foreach (var line in jsonLines)
                            {
                                if (line.Contains("FirstName") || line.Contains("LastName") || 
                                    line.Contains("Plant_Name") || line.Contains("Department_Name") ||
                                    line.Contains("User_Code") || line.Contains("User_Email"))
                                {
                                    Console.WriteLine($"[API-FIELD] {line.Trim()}");
                                }
                            }
                            
                            // Try simple regex extraction first and log results
                            var patterns = new Dictionary<string, string>
                            {
                                {"Detail_TH_FirstName", "\"Detail_TH_FirstName\"\\s*:\\s*\"([^\"]+)\""},
                                {"Detail_TH_LastName", "\"Detail_TH_LastName\"\\s*:\\s*\"([^\"]+)\""},
                                {"Detail_EN_FirstName", "\"Detail_EN_FirstName\"\\s*:\\s*\"([^\"]+)\""},
                                {"Detail_EN_LastName", "\"Detail_EN_LastName\"\\s*:\\s*\"([^\"]+)\""},
                                {"Plant_Name", "\"Plant_Name\"\\s*:\\s*\"([^\"]+)\""},
                                {"Department_Name", "\"Department_Name\"\\s*:\\s*\"([^\"]+)\""},
                                {"User_Code", "\"User_Code\"\\s*:\\s*\"([^\"]+)\""},
                                {"User_Email", "\"User_Email\"\\s*:\\s*\"([^\"]+)\""}
                            };
                            
                            Console.WriteLine("[API-EXTRACT] Regex extraction results:");
                            foreach (var pattern in patterns)
                            {
                                var match = Regex.Match(jsonContent, pattern.Value);
                                if (match.Success && match.Groups.Count > 1)
                                {
                                    Console.WriteLine($"[API-FOUND] {pattern.Key}: {match.Groups[1].Value}");
                                    
                                    // Update userInfo based on pattern
                                    switch (pattern.Key)
                                    {
                                        case "Detail_TH_FirstName": userInfo.ThaiFirstName = match.Groups[1].Value; break;
                                        case "Detail_TH_LastName": userInfo.ThaiLastName = match.Groups[1].Value; break;
                                        case "Detail_EN_FirstName": userInfo.EnglishFirstName = match.Groups[1].Value; break;
                                        case "Detail_EN_LastName": userInfo.EnglishLastName = match.Groups[1].Value; break;
                                        case "Plant_Name": userInfo.PlantName = match.Groups[1].Value; break;
                                        case "Department_Name": userInfo.Department = match.Groups[1].Value; break;
                                        case "User_Code": userInfo.UserCode = match.Groups[1].Value; break;
                                        case "User_Email": userInfo.Email = match.Groups[1].Value; break;
                                    }
                                }
                            }
                            
                            // Try JsonDocument parsing as fallback
                            Console.WriteLine("[API-JSON] Attempting JsonDocument parse");
                            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                            {
                                JsonElement root = doc.RootElement;
                                Console.WriteLine($"[API-JSON] Root element properties: {string.Join(", ", root.EnumerateObject().Select(p => p.Name))}");
                                
                                // Recursively print all nested properties with values
                                void PrintJsonProperties(JsonElement element, string path = "")
                                {
                                    if (element.ValueKind == JsonValueKind.Object)
                                    {
                                        foreach (var property in element.EnumerateObject())
                                        {
                                            string newPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                                            if (property.Value.ValueKind == JsonValueKind.String || 
                                                property.Value.ValueKind == JsonValueKind.Number ||
                                                property.Value.ValueKind == JsonValueKind.True ||
                                                property.Value.ValueKind == JsonValueKind.False)
                                            {
                                                Console.WriteLine($"[API-JSON-PROP] {newPath}: {property.Value}");
                                            }
                                            PrintJsonProperties(property.Value, newPath);
                                        }
                                    }
                                    else if (element.ValueKind == JsonValueKind.Array)
                                    {
                                        int index = 0;
                                        foreach (var item in element.EnumerateArray())
                                        {
                                            PrintJsonProperties(item, $"{path}[{index}]");
                                            index++;
                                        }
                                    }
                                }
                                
                                PrintJsonProperties(root);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[API-ERROR] JSON parsing failed: {ex.Message}");
                        }
                        
                        // Log final extracted values
                        Console.WriteLine("[API-RESULTS] Final extracted values:");
                        Console.WriteLine($"UserCode: {userInfo.UserCode}");
                        Console.WriteLine($"ThaiFirstName: {userInfo.ThaiFirstName}");
                        Console.WriteLine($"ThaiLastName: {userInfo.ThaiLastName}");
                        Console.WriteLine($"EnglishFirstName: {userInfo.EnglishFirstName}");
                        Console.WriteLine($"EnglishLastName: {userInfo.EnglishLastName}");
                        Console.WriteLine($"Department: {userInfo.Department}");
                        Console.WriteLine($"PlantName: {userInfo.PlantName}");
                        Console.WriteLine($"Email: {userInfo.Email}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API-EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[API-EXCEPTION] Stack: {ex.StackTrace}");
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
                // Special handling for admin user
                if (username.ToLower() == "admin" && password == "admin")
                {
                    result.Add("SamAccountName", "admin");
                    result.Add("DisplayName", "Administrator");
                    result.Add("Email", "admin@thaiparker.co.th");
                    result.Add("Department", "IT Department");
                    result.Add("UserCode", "1670660");
                    result.Add("RawJsonData", @"{""Detail_TH_FirstName"":""แอดมิน"",""Detail_TH_LastName"":""ทดสอบ"",""Detail_EN_FirstName"":""Admin"",""Detail_EN_LastName"":""Test""}");
                    result.Add("ThaiFirstName", "แอดมิน");
                    result.Add("ThaiLastName", "ทดสอบ");
                    result.Add("EnglishFirstName", "Admin");
                    result.Add("EnglishLastName", "Test");
                    return result;
                }
                
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    bool isValid = false;
                    try {
                        isValid = context.ValidateCredentials(username, password);
                    } catch (Exception authEx) {
                        result.Add("AUTH_ERROR", authEx.Message);
                    }
                    
                    // Try to get user information regardless of authentication
                    try
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                // Add basic properties
                                result.Add("SamAccountName", user.SamAccountName ?? "");
                                result.Add("EmailAddress", user.EmailAddress ?? "");
                                result.Add("DisplayName", user.DisplayName ?? "");
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
                                            
                                            // Skip extremely long values
                                            if (propValue.Length > 1000)
                                            {
                                                propValue = $"[Binary data, length: {propValue.Length}]";
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
                    catch (Exception userEx)
                    {
                        result.Add("USER_ERROR", userEx.Message);
                    }
                    
                    // Add authentication result
                    result.Add("AUTHENTICATED", isValid.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving LDAP attributes: {ex.Message}");
                result.Add("ERROR", ex.Message);
                result.Add("StackTrace", ex.StackTrace?.ToString() ?? "No stack trace available");
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