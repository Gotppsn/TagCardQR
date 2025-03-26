// Path: Controllers/UserProfileController.cs
using System.Threading.Tasks;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly UserProfileService _userProfileService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            UserProfileService userProfileService,
            ILogger<UserProfileController> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        // GET: UserProfile
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var userProfile = await _userProfileService.GetUserProfileAsync(username);
            if (userProfile == null)
            {
                // This shouldn't happen if login process is working correctly
                _logger.LogWarning($"User profile not found for authenticated user: {username}");
                return RedirectToAction("Login", "Account");
            }

            return View(userProfile);
        }
    }
}