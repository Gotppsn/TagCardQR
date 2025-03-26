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
        var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

        if (isValid)
        {
            _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}");
            
            // Extract Thai names from LDAP data if available
            string thaiFirstName = "";
            string thaiLastName = "";
            
            // Check if there's JSON data available from LDAP
            if (!string.IsNullOrEmpty(userInfo.RawJsonData))
            {
                var thFirstNameMatch = System.Text.RegularExpressions.Regex.Match(
                    userInfo.RawJsonData, "\"Detail_TH_FirstName\":\"([^\"]+)\"");
                if (thFirstNameMatch.Success && thFirstNameMatch.Groups.Count > 1)
                {
                    thaiFirstName = thFirstNameMatch.Groups[1].Value;
                }
                
                var thLastNameMatch = System.Text.RegularExpressions.Regex.Match(
                    userInfo.RawJsonData, "\"Detail_TH_LastName\":\"([^\"]+)\"");
                if (thLastNameMatch.Success && thLastNameMatch.Groups.Count > 1)
                {
                    thaiLastName = thLastNameMatch.Groups[1].Value;
                }
            }
            
            await _userProfileService.CreateUserProfileIfNotExistsAsync(
                username: model.Username,
                firstName: userInfo.FullName?.Split(' ').FirstOrDefault() ?? "",
                lastName: userInfo.FullName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                email: userInfo.Email ?? "",
                department: userInfo.Department ?? "",
                plant: userInfo.PlantName ?? "",
                userCode: userInfo.UserCode ?? "",
                thaiFirstName: thaiFirstName,
                thaiLastName: thaiLastName
            );
            
            // Rest of authentication code remains the same
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, model.Username == "admin" ? "Administrator" : "User"),
                new Claim("Username", userInfo.Username),
                new Claim("Department", userInfo.Department ?? ""),
                new Claim("Email", userInfo.Email ?? ""),
                new Claim("FullName", userInfo.FullName ?? ""),
                new Claim("PlantName", userInfo.PlantName ?? ""),
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
    return View(model);
}

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Account/Login?ReturnUrl=/");
        }
    }
}