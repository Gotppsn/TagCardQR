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
                    
                    // Get LDAP attributes for debugging if needed
                    Dictionary<string, string> ldapAttributes = await _authService.GetAllLdapAttributesAsync(model.Username, model.Password);
                    ViewBag.LdapDebugData = System.Text.Json.JsonSerializer.Serialize(ldapAttributes);
                    
                    // Validate credentials
                    var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

                    if (isValid)
                    {
                        _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}");
                        
                        // Get additional data from API if user code exists
                        if (!string.IsNullOrEmpty(userInfo.UserCode))
                        {
                            var apiUserInfo = await _authService.GetUserDataFromApiAsync(userInfo.UserCode);
                            if (apiUserInfo != null)
                            {
                                // Force all values from API
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.ThaiFirstName)) 
                                    userInfo.ThaiFirstName = apiUserInfo.ThaiFirstName;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.ThaiLastName)) 
                                    userInfo.ThaiLastName = apiUserInfo.ThaiLastName;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.EnglishFirstName)) 
                                    userInfo.EnglishFirstName = apiUserInfo.EnglishFirstName;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.EnglishLastName)) 
                                    userInfo.EnglishLastName = apiUserInfo.EnglishLastName;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.Email)) 
                                    userInfo.Email = apiUserInfo.Email;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.Department)) 
                                    userInfo.Department = apiUserInfo.Department;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.PlantName)) 
                                    userInfo.PlantName = apiUserInfo.PlantName;
                                if (!string.IsNullOrWhiteSpace(apiUserInfo.UserCode)) 
                                    userInfo.UserCode = apiUserInfo.UserCode;
                                
                                // Preserve raw JSON
                                userInfo.RawJsonData = apiUserInfo.RawJsonData;
                            }
                        }
                        
                        _logger.LogInformation($"Creating profile with TH names: {userInfo.ThaiFirstName} {userInfo.ThaiLastName}, Plant: {userInfo.PlantName}");
                        
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
                        
                        // Force direct database update for critical fields
                        await _userProfileService.ForceUpdateUserProfileAsync(model.Username, userInfo);
                        
                        // Create authentication claims with user data
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim("Department", userInfo.Department ?? ""),
                            new Claim("Email", userInfo.Email ?? ""),
                            new Claim("User_Code", userInfo.UserCode ?? ""),
                            new Claim("EN_FirstName", userInfo.EnglishFirstName ?? ""),
                            new Claim("EN_LastName", userInfo.EnglishLastName ?? ""),
                            new Claim("TH_FirstName", userInfo.ThaiFirstName ?? ""),
                            new Claim("TH_LastName", userInfo.ThaiLastName ?? ""),
                            new Claim("PlantName", userInfo.PlantName ?? ""),
                            new Claim("login_timestamp", DateTime.UtcNow.Ticks.ToString())
                        };

                        // Get user roles and add them to claims
                        var userRoles = await _roleService.GetUserRolesAsync(userProfile.Id);
                        foreach (var role in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.Name));
                        }

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        
                        // Complete authentication
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                            });

                        return RedirectToAction("Index", "Card");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login process");
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