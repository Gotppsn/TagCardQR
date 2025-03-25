// Path: Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CardTagManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly LdapAuthenticationService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(LdapAuthenticationService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            // Always set a default returnUrl
            ViewData["ReturnUrl"] = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            return View(new LoginViewModel { ReturnUrl = ViewData["ReturnUrl"]?.ToString() });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var (isValid, userInfo) = _authService.ValidateCredentials(model.Username, model.Password);

                if (isValid)
                {
                    _logger.LogInformation($"User authenticated: {model.Username}, UserCode: {userInfo.UserCode}");
                    
                    // Ensure we're creating a fresh claims identity each time
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username),
                        new Claim(ClaimTypes.Role, model.Username == "admin" ? "Administrator" : "User"),
                        // Add user LDAP information as claims
                        new Claim("Username", userInfo.Username),
                        new Claim("Department", userInfo.Department ?? ""),
                        new Claim("Email", userInfo.Email ?? ""),
                        new Claim("FullName", userInfo.FullName ?? ""),
                        new Claim("PlantName", userInfo.PlantName ?? ""),
                        // CRITICAL: This is the claim we need for CreatedByID/UpdatedByID
                        new Claim("User_Code", userInfo.UserCode ?? ""), 
                        // Add unique timestamp claim to prevent stale authentication
                        new Claim("login_timestamp", DateTime.UtcNow.Ticks.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Explicitly set IsPersistent based on RememberMe
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = System.DateTimeOffset.UtcNow.AddHours(12),
                        // Adding redirect URL to auth properties
                        RedirectUri = returnUrl
                    };

                    // Ensure user is completely signed out before signing in again
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Card");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }
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