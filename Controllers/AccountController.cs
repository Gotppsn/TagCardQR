// Path: Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;

namespace CardTagManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly LdapAuthenticationService _authService;
        private readonly UserProfileService _userProfileService;
        private readonly RoleService _roleService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            LdapAuthenticationService authService, 
            UserProfileService userProfileService,
            RoleService roleService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            return View(new LoginViewModel { ReturnUrl = ViewData["ReturnUrl"]?.ToString() });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation($"Login attempt for user: {model.Username}");
                    
                    // Extract LDAP attributes regardless of authentication result
                    Dictionary<string, string> ldapAttributes = await _authService.GetAllLdapAttributesAsync(model.Username, model.Password);
                    ViewBag.LdapDebugData = System.Text.Json.JsonSerializer.Serialize(ldapAttributes);
                    
                    // Store LDAP data in TempData to persist across redirects
                    TempData["LdapDebugData"] = System.Text.Json.JsonSerializer.Serialize(ldapAttributes);
                    
                    // Proceed with normal authentication
                    var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

                    if (isValid)
                    {
                        _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}");
                        
                        // Get additional user data from API if user code exists
                        if (!string.IsNullOrEmpty(userInfo.UserCode))
                        {
                            var apiUserInfo = await _authService.GetUserDataFromApiAsync(userInfo.UserCode);
                            if (apiUserInfo != null)
                            {
                                // Merge data prioritizing API values over LDAP values when available
                                userInfo.ThaiFirstName = !string.IsNullOrWhiteSpace(apiUserInfo.ThaiFirstName) 
                                    ? apiUserInfo.ThaiFirstName : userInfo.ThaiFirstName;
                                userInfo.ThaiLastName = !string.IsNullOrWhiteSpace(apiUserInfo.ThaiLastName) 
                                    ? apiUserInfo.ThaiLastName : userInfo.ThaiLastName;
                                userInfo.EnglishFirstName = !string.IsNullOrWhiteSpace(apiUserInfo.EnglishFirstName) 
                                    ? apiUserInfo.EnglishFirstName : userInfo.EnglishFirstName;
                                userInfo.EnglishLastName = !string.IsNullOrWhiteSpace(apiUserInfo.EnglishLastName) 
                                    ? apiUserInfo.EnglishLastName : userInfo.EnglishLastName;
                                userInfo.Email = !string.IsNullOrWhiteSpace(apiUserInfo.Email) 
                                    ? apiUserInfo.Email : userInfo.Email;
                                userInfo.Department = !string.IsNullOrWhiteSpace(apiUserInfo.Department) 
                                    ? apiUserInfo.Department : userInfo.Department;
                                userInfo.PlantName = !string.IsNullOrWhiteSpace(apiUserInfo.PlantName) 
                                    ? apiUserInfo.PlantName : userInfo.PlantName;
                                    
                                // Preserve raw JSON for further extraction if needed
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.RawJsonData))
                                    userInfo.RawJsonData = apiUserInfo.RawJsonData;
                            }
                        }
                        
                        // Create/update profile in database
                        var userProfile = await _userProfileService.CreateUserProfileIfNotExistsAsync(
                            username: model.Username,
                            firstName: userInfo.EnglishFirstName,
                            lastName: userInfo.EnglishLastName,
                            email: userInfo.Email ?? "",
                            department: userInfo.Department ?? "",
                            plant: userInfo.PlantName ?? "",
                            userCode: userInfo.UserCode ?? "",
                            thaiFirstName: userInfo.ThaiFirstName,
                            thaiLastName: userInfo.ThaiLastName,
                            rawJsonData: userInfo.RawJsonData
                        );
                        
                        if (userProfile == null)
                        {
                            _logger.LogError($"Failed to create user profile for {model.Username}");
                            ModelState.AddModelError(string.Empty, "Failed to create user profile. Please try again.");
                            return View(model);
                        }

                        // Get user roles
                        var userRoles = await _roleService.GetUserRolesAsync(userProfile.Id);
                        
                        // Special case for admin user - ensure they have admin role
                        if (model.Username.ToLower() == "admin" && !userRoles.Any(r => r.NormalizedName == "ADMIN"))
                        {
                            var adminRole = await _roleService.GetRoleByNameAsync("ADMIN");
                            if (adminRole != null)
                            {
                                await _roleService.AssignRoleToUserAsync(userProfile.Id, adminRole.Id, "System");
                                userRoles.Add(adminRole);
                            }
                        }
                        
                        // If no roles assigned, try assigning default User role
                        if (!userRoles.Any())
                        {
                            var userRole = await _roleService.GetRoleByNameAsync("USER");
                            if (userRole != null)
                            {
                                await _roleService.AssignRoleToUserAsync(userProfile.Id, userRole.Id, "System");
                                userRoles.Add(userRole);
                            }
                        }

                        // Create authentication claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim("Username", userInfo.Username),
                            new Claim("Department", userInfo.Department ?? ""),
                            new Claim("Email", userInfo.Email ?? ""),
                            new Claim("User_Code", userInfo.UserCode ?? ""),
                            new Claim("EN_FirstName", userInfo.EnglishFirstName ?? ""),
                            new Claim("EN_LastName", userInfo.EnglishLastName ?? ""),
                            new Claim("TH_FirstName", userInfo.ThaiFirstName ?? ""),
                            new Claim("TH_LastName", userInfo.ThaiLastName ?? ""),
                            new Claim("login_timestamp", DateTime.UtcNow.Ticks.ToString())
                        };
                        
                        // Add role claims
                        foreach (var role in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.Name));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
                            RedirectUri = returnUrl
                        };

                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                                                    new ClaimsPrincipal(claimsIdentity), 
                                                    authProperties);

                        // Redirect only if not in debug mode (LDAP attributes shown)
                        var showDebug = Request.Form.ContainsKey("debug") || Request.Query.ContainsKey("debug");
                        if (!showDebug)
                        {
                            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) 
                                ? Redirect(returnUrl) 
                                : RedirectToAction("Index", "Card");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login process for user {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Login process failed. Please try again.");
                }
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Account/Login?ReturnUrl=/");
        }
    }
}