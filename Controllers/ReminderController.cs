// Path: Controllers/ReminderController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReminderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReminderController> _logger;

        public ReminderController(ApplicationDbContext context, ILogger<ReminderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Reminder/card/5
        [AllowAnonymous]
        [HttpGet("card/{cardId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceReminder>>> GetCardReminders(int cardId)
        {
            try
            {
                // Check if this card is private
                var scanSettings = await _context.ScanSettings
                    .FirstOrDefaultAsync(s => s.CardId == cardId);
                
                bool privateMode = scanSettings?.PrivateMode ?? false;
                
                // If private mode and user not authenticated, return limited data or error
                if (privateMode && !User.Identity.IsAuthenticated)
                {
                    return StatusCode(401, new { error = "Authentication required", requiresAuth = true });
                }
                
                var reminders = await _context.MaintenanceReminders
                    .Where(r => r.CardId == cardId)
                    .OrderBy(r => r.DueDate)
                    .ToListAsync();
                
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving reminders for card {cardId}");
                return StatusCode(500, new { error = "An error occurred while retrieving reminders." });
            }
        }

        // GET: api/Reminder/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceReminder>> GetReminder(int id)
        {
            var reminder = await _context.MaintenanceReminders.FindAsync(id);

            if (reminder == null)
            {
                return NotFound();
            }

            return Ok(reminder);
        }

        // POST: api/Reminder - Create new reminder
        [HttpPost]
        public async Task<ActionResult<MaintenanceReminder>> CreateReminder([FromBody] MaintenanceReminder reminder)
        {
            _logger.LogInformation("Received reminder request with data: {Title}, {DueDate}", 
                reminder?.Title, reminder?.DueDate);
                
            try
            {
                // Explicitly remove Card validation to prevent navigation property validation errors
                if (ModelState.ContainsKey("Card"))
                {
                    ModelState.Remove("Card");
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                        
                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { errors });
                }

                // Verify the card exists before creating the reminder
                var card = await _context.Cards.FindAsync(reminder.CardId);
                if (card == null)
                {
                    return NotFound(new { error = "Card not found" });
                }
                
                // Set metadata on the reminder
                reminder.CreatedAt = DateTime.Now;
                reminder.UpdatedAt = DateTime.Now;
                
                // Set creator info with user identity for audit trail
                reminder.CreatedBy = User.Identity?.Name ?? "system";
                
                // Add User_Code for better tracking
                var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                if (userCodeClaim != null)
                {
                    reminder.CreatedBy += $" (ID: {userCodeClaim.Value})";
                }
                
                _context.MaintenanceReminders.Add(reminder);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Reminder created successfully with ID: {Id}", reminder.Id);

                return CreatedAtAction(nameof(GetReminder), new { id = reminder.Id }, reminder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reminder: {Message}", ex.Message);
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Reminder/5 - Update existing reminder
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReminder(int id, MaintenanceReminder reminder)
        {
            if (id != reminder.Id)
            {
                return BadRequest();
            }

            // Remove Card validation to avoid the same navigation property issue
            if (ModelState.ContainsKey("Card"))
            {
                ModelState.Remove("Card");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingReminder = await _context.MaintenanceReminders.FindAsync(id);
                
                if (existingReminder == null)
                {
                    return NotFound();
                }
                
                // Update only the fields that should be modifiable
                existingReminder.Title = reminder.Title;
                existingReminder.DueDate = reminder.DueDate;
                existingReminder.Notes = reminder.Notes;
                existingReminder.RepeatFrequency = reminder.RepeatFrequency;
                existingReminder.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating reminder {id}");
                return StatusCode(500, new { error = "An error occurred while updating the reminder." });
            }
        }

        // DELETE: api/Reminder/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReminder(int id)
        {
            try
            {
                var reminder = await _context.MaintenanceReminders.FindAsync(id);
                
                if (reminder == null)
                {
                    return NotFound();
                }
                
                _context.MaintenanceReminders.Remove(reminder);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting reminder {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the reminder." });
            }
        }
    }
}