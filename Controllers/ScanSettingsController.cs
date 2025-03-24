// Path: Controllers/ScanSettingsController.cs
using System;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScanSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScanSettingsController> _logger;

        public ScanSettingsController(ApplicationDbContext context, ILogger<ScanSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("card/{cardId}")]
        public async Task<IActionResult> GetCardScanSettings(int cardId)
        {
            try
            {
                var settings = await _context.ScanSettings
                    .FirstOrDefaultAsync(s => s.CardId == cardId);
                
                if (settings == null)
                    return NotFound(new { error = "No scan settings found for this card." });
                
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving scan settings for card {cardId}");
                return StatusCode(500, new { error = "An error occurred while retrieving scan settings." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveScanSettings(ScanSettings settings)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingSettings = await _context.ScanSettings
                    .FirstOrDefaultAsync(s => s.CardId == settings.CardId);
                
                if (existingSettings != null)
                {
                    existingSettings.FieldsJson = settings.FieldsJson;
                    existingSettings.UiElementsJson = settings.UiElementsJson;
                    existingSettings.PrivateMode = settings.PrivateMode;
                    existingSettings.UpdatedAt = DateTime.Now;
                    
                    _context.ScanSettings.Update(existingSettings);
                }
                else
                {
                    settings.CreatedAt = DateTime.Now;
                    settings.UpdatedAt = DateTime.Now;
                    settings.CreatedBy = User.Identity?.Name ?? string.Empty;
                    
                    _context.ScanSettings.Add(settings);
                }
                
                await _context.SaveChangesAsync();
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scan settings");
                return StatusCode(500, new { error = "An error occurred while saving scan settings." });
            }
        }

        [AllowAnonymous]
        [HttpGet("public/card/{cardId}")]
        public async Task<IActionResult> GetPublicCardScanSettings(int cardId)
        {
            try
            {
                var settings = await _context.ScanSettings
                    .FirstOrDefaultAsync(s => s.CardId == cardId);
                
                if (settings == null)
                    return NotFound(new { error = "No scan settings found for this card." });
                
                return Ok(new
                {
                    fields = settings.FieldsJson,
                    uiElements = settings.UiElementsJson,
                    privateMode = settings.PrivateMode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving public scan settings for card {cardId}");
                return StatusCode(500, new { error = "An error occurred while retrieving scan settings." });
            }
        }
    }
}