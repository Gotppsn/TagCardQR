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
        [HttpGet("card/{cardId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceReminder>>> GetCardReminders(int cardId)
        {
            try
            {
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

        // POST: api/Reminder
        [HttpPost]
        public async Task<ActionResult<MaintenanceReminder>> CreateReminder(MaintenanceReminder reminder)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                reminder.CreatedAt = DateTime.Now;
                reminder.UpdatedAt = DateTime.Now;
                
                // Set creator info
                reminder.CreatedBy = User.Identity?.Name ?? "system";
                
                // Get User_Code from claims if available
                var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                if (userCodeClaim != null)
                {
                    reminder.CreatedBy += $" (ID: {userCodeClaim.Value})";
                }
                
                _context.MaintenanceReminders.Add(reminder);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReminder), new { id = reminder.Id }, reminder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reminder");
                return StatusCode(500, new { error = "An error occurred while creating the reminder." });
            }
        }

        // PUT: api/Reminder/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReminder(int id, MaintenanceReminder reminder)
        {
            if (id != reminder.Id)
            {
                return BadRequest();
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