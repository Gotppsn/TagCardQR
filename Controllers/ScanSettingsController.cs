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
        public async Task<IActionResult> SaveScanSettings([FromBody] dynamic requestData)
        {
            try
            {
                // Extract values from dynamic request
                int cardId = Convert.ToInt32(requestData.GetProperty("cardId").GetInt32());
                string fieldsJson = requestData.GetProperty("fieldsJson").GetString();
                string uiElementsJson = requestData.GetProperty("uiElementsJson").GetString();
                bool privateMode = requestData.GetProperty("privateMode").GetBoolean();

                _logger.LogInformation($"Saving scan settings: CardId={cardId}, Fields={fieldsJson}, UI={uiElementsJson}, Private={privateMode}");

                // Find existing or create new
                var existingSettings = await _context.ScanSettings
                    .FirstOrDefaultAsync(s => s.CardId == cardId);

                if (existingSettings != null)
                {
                    existingSettings.FieldsJson = fieldsJson;
                    existingSettings.UiElementsJson = uiElementsJson;
                    existingSettings.PrivateMode = privateMode;
                    existingSettings.UpdatedAt = DateTime.Now;
                    _context.ScanSettings.Update(existingSettings);
                }
                else
                {
                    var newSettings = new ScanSettings
                    {
                        CardId = cardId,
                        FieldsJson = fieldsJson,
                        UiElementsJson = uiElementsJson,
                        PrivateMode = privateMode,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        CreatedBy = User.Identity?.Name ?? string.Empty
                    };
                    _context.ScanSettings.Add(newSettings);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scan settings");
                return StatusCode(500, new { error = ex.Message });
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