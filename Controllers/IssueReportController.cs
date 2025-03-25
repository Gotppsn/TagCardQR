// Path: Controllers/IssueReportController.cs
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class IssueReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IssueReportController> _logger;

        public IssueReportController(ApplicationDbContext context, ILogger<IssueReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/IssueReport/card/5
        [HttpGet("card/{cardId}")]
        public async Task<ActionResult<IEnumerable<IssueReport>>> GetCardIssues(int cardId)
        {
            try
            {
                var issues = await _context.IssueReports
                    .Where(r => r.CardId == cardId)
                    .OrderByDescending(r => r.ReportDate)
                    .ToListAsync();
                
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving issues for card {cardId}");
                return StatusCode(500, new { error = "An error occurred while retrieving issues." });
            }
        }

        // GET: api/IssueReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IssueReport>> GetIssue(int id)
        {
            var issue = await _context.IssueReports.FindAsync(id);

            if (issue == null)
            {
                return NotFound();
            }

            return Ok(issue);
        }

        // POST: api/IssueReport
        [HttpPost]
        public async Task<ActionResult<IssueReport>> CreateIssue([FromBody] IssueReport issue)
        {
            try 
            {
                _logger.LogInformation($"Received issue report: {System.Text.Json.JsonSerializer.Serialize(issue)}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning($"Validation failed: {string.Join(", ", errors)}");
                    return BadRequest(new { errors });
                }
                
                // Validate the card exists
                var card = await _context.Cards.FindAsync(issue.CardId);
                if (card == null)
                {
                    return NotFound(new { error = "Product not found" });
                }
                
                // Set default values for missing fields
                issue.Status ??= "Open";
                issue.ReporterPhone ??= string.Empty;
                issue.Resolution ??= string.Empty;
                issue.CreatedAt = DateTime.Now;
                
                // Add to database
                _context.IssueReports.Add(issue);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Issue report created successfully with ID: {issue.Id}");
                
                return CreatedAtAction(nameof(GetIssue), new { id = issue.Id }, issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating issue report for card {issue?.CardId}");
                return StatusCode(500, new { 
                    error = "An error occurred while creating the issue report",
                    details = ex.Message 
                });
            }
        }


        // PUT: api/IssueReport/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIssue(int id, IssueReport issue)
        {
            if (id != issue.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingIssue = await _context.IssueReports.FindAsync(id);
                
                if (existingIssue == null)
                {
                    return NotFound();
                }
                
                existingIssue.IssueType = issue.IssueType;
                existingIssue.Priority = issue.Priority;
                existingIssue.Description = issue.Description;
                existingIssue.ReportDate = issue.ReportDate;
                existingIssue.ReporterName = issue.ReporterName;
                existingIssue.ReporterEmail = issue.ReporterEmail;
                existingIssue.ReporterPhone = issue.ReporterPhone;
                existingIssue.Status = issue.Status;
                existingIssue.Resolution = issue.Resolution;
                
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating issue {id}");
                return StatusCode(500, new { error = "An error occurred while updating the issue." });
            }
        }

        // DELETE: api/IssueReport/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            try
            {
                var issue = await _context.IssueReports.FindAsync(id);
                
                if (issue == null)
                {
                    return NotFound();
                }
                
                _context.IssueReports.Remove(issue);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting issue {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the issue." });
            }
        }
    }
}