// Path: Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly LdapAuthenticationService _authService;
        private readonly UserProfileService _userProfileService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            LdapAuthenticationService authService, 
            UserProfileService userProfileService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userProfileService = userProfileService;
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
    if (ModelState.IsValid)
    {
        try
        {
            _logger.LogInformation($"Login attempt for user: {model.Username}");
            var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

            if (isValid)
            {
                _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}, " +
                    $"ThaiFirstName: {userInfo.ThaiFirstName}, ThaiLastName: {userInfo.ThaiLastName}, " +
                    $"EnglishFirstName: {userInfo.EnglishFirstName}, EnglishLastName: {userInfo.EnglishLastName}");
                
                // Always try to get user data from API if user code is available
                if (!string.IsNullOrEmpty(userInfo.UserCode))
                {
                    var apiUserInfo = await _authService.GetUserDataFromApiAsync(userInfo.UserCode);
                    if (apiUserInfo != null)
                    {
                        _logger.LogInformation($"Retrieved API data for user: {model.Username}, " +
                            $"TH: {apiUserInfo.ThaiFirstName} {apiUserInfo.ThaiLastName}, " +
                            $"EN: {apiUserInfo.EnglishFirstName} {apiUserInfo.EnglishLastName}, " +
                            $"Code: {apiUserInfo.UserCode}");
                        
                        // Merge API data with LDAP data
                        if (string.IsNullOrWhiteSpace(userInfo.ThaiFirstName) && !string.IsNullOrWhiteSpace(apiUserInfo.ThaiFirstName))
                            userInfo.ThaiFirstName = apiUserInfo.ThaiFirstName;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.ThaiLastName) && !string.IsNullOrWhiteSpace(apiUserInfo.ThaiLastName))
                            userInfo.ThaiLastName = apiUserInfo.ThaiLastName;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.EnglishFirstName) && !string.IsNullOrWhiteSpace(apiUserInfo.EnglishFirstName))
                            userInfo.EnglishFirstName = apiUserInfo.EnglishFirstName;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.EnglishLastName) && !string.IsNullOrWhiteSpace(apiUserInfo.EnglishLastName))
                            userInfo.EnglishLastName = apiUserInfo.EnglishLastName;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.Email) && !string.IsNullOrWhiteSpace(apiUserInfo.Email))
                            userInfo.Email = apiUserInfo.Email;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.Department) && !string.IsNullOrWhiteSpace(apiUserInfo.Department))
                            userInfo.Department = apiUserInfo.Department;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.PlantName) && !string.IsNullOrWhiteSpace(apiUserInfo.PlantName))
                            userInfo.PlantName = apiUserInfo.PlantName;
                            
                        if (string.IsNullOrWhiteSpace(userInfo.RawJsonData) && !string.IsNullOrWhiteSpace(apiUserInfo.RawJsonData))
                            userInfo.RawJsonData = apiUserInfo.RawJsonData;
                    }
                }
                
                // Create or update user profile with merged data
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
                    _logger.LogError($"Failed to create/update user profile for {model.Username}");
                    ModelState.AddModelError(string.Empty, "Failed to create user profile. Please try again.");
                    return View(model);
                }

                // Authentication claims - add the Thai/English name claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, model.Username == "admin" ? "Administrator" : "User"),
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

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = System.DateTimeOffset.UtcNow.AddHours(12),
                    RedirectUri = returnUrl
                };

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) 
                    ? Redirect(returnUrl) 
                    : RedirectToAction("Index", "Card");
            }
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        }
    }
    
    ViewData["ReturnUrl"] = returnUrl;
    return View(model);
}

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Account/Login?ReturnUrl=/");
        }
    }
}