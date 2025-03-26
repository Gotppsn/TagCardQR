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
            var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

            if (isValid)
            {
                _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}");
                
                // Fetch additional user data from API if user code is available
                if (!string.IsNullOrEmpty(userInfo.UserCode))
                {
                    var apiUserInfo = await _authService.GetUserDataFromApiAsync(userInfo.UserCode);
                    if (apiUserInfo != null)
                    {
                        // Merge API data with LDAP data
                        if (string.IsNullOrEmpty(userInfo.ThaiFirstName) && !string.IsNullOrEmpty(apiUserInfo.ThaiFirstName))
                            userInfo.ThaiFirstName = apiUserInfo.ThaiFirstName;
                            
                        if (string.IsNullOrEmpty(userInfo.ThaiLastName) && !string.IsNullOrEmpty(apiUserInfo.ThaiLastName))
                            userInfo.ThaiLastName = apiUserInfo.ThaiLastName;
                            
                        if (string.IsNullOrEmpty(userInfo.EnglishFirstName) && !string.IsNullOrEmpty(apiUserInfo.EnglishFirstName))
                            userInfo.EnglishFirstName = apiUserInfo.EnglishFirstName;
                            
                        if (string.IsNullOrEmpty(userInfo.EnglishLastName) && !string.IsNullOrEmpty(apiUserInfo.EnglishLastName))
                            userInfo.EnglishLastName = apiUserInfo.EnglishLastName;
                            
                        if (string.IsNullOrEmpty(userInfo.Email) && !string.IsNullOrEmpty(apiUserInfo.Email))
                            userInfo.Email = apiUserInfo.Email;
                            
                        if (string.IsNullOrEmpty(userInfo.Department) && !string.IsNullOrEmpty(apiUserInfo.Department))
                            userInfo.Department = apiUserInfo.Department;
                            
                        if (string.IsNullOrEmpty(userInfo.PlantName) && !string.IsNullOrEmpty(apiUserInfo.PlantName))
                            userInfo.PlantName = apiUserInfo.PlantName;
                            
                        if (string.IsNullOrEmpty(userInfo.RawJsonData) && !string.IsNullOrEmpty(apiUserInfo.RawJsonData))
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

                // Authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, model.Username == "admin" ? "Administrator" : "User"),
                    new Claim("Username", userInfo.Username),
                    new Claim("Department", userInfo.Department ?? ""),
                    new Claim("Email", userInfo.Email ?? ""),
                    new Claim("User_Code", userInfo.UserCode ?? ""),
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