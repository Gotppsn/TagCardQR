// Path: Controllers/TemplateController.cs
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
using System.Security.Claims;

namespace CardTagManager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(ApplicationDbContext context, ILogger<TemplateController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Template
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Template>>> GetTemplates()
        {
            try
            {
                return await _context.Templates.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                return StatusCode(500, new { error = "An error occurred while retrieving templates." });
            }
        }

        // GET: api/Template/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Template>> GetTemplate(int id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);

                if (template == null)
                {
                    return NotFound();
                }

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving template {id}");
                return StatusCode(500, new { error = "An error occurred while retrieving the template." });
            }
        }

        // POST: api/Template
        [HttpPost]
        public async Task<ActionResult<Template>> CreateTemplate(Template template)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get user info from claims
                string username = User.Identity?.Name ?? "system";
                string userCode = User.Claims.FirstOrDefault(c => c.Type == "User_Code")?.Value ?? "";
                
                _logger.LogInformation($"Creating template with user: {username}, code: {userCode}");

                template.CreatedBy = username;
                template.CreatedByID = userCode;
                template.UpdatedBy = username;
                template.UpdatedByID = userCode;
                template.CreatedAt = DateTime.Now;
                template.UpdatedAt = DateTime.Now;

                // Ensure IconColor has a default value if not provided
                if (string.IsNullOrEmpty(template.IconColor))
                {
                    template.IconColor = "primary-500";
                }

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                return StatusCode(500, new { error = "An error occurred while creating the template." });
            }
        }

        // PUT: api/Template/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, Template template)
        {
            if (id != template.Id)
            {
                return BadRequest();
            }

            try
            {
                var existingTemplate = await _context.Templates.FindAsync(id);
                
                if (existingTemplate == null)
                {
                    return NotFound();
                }

                // Get user info from claims
                string username = User.Identity?.Name ?? "system";
                string userCode = User.Claims.FirstOrDefault(c => c.Type == "UserCode")?.Value ?? "";
                
                _logger.LogInformation($"Updating template with user: {username}, code: {userCode}");

                existingTemplate.Name = template.Name;
                existingTemplate.Category = template.Category;
                existingTemplate.Icon = template.Icon;
                existingTemplate.BgColor = template.BgColor;
                existingTemplate.IconColor = template.IconColor ?? "primary-500";
                existingTemplate.FieldsJson = template.FieldsJson;
                existingTemplate.UpdatedAt = DateTime.Now;
                existingTemplate.UpdatedBy = username;
                existingTemplate.UpdatedByID = userCode;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating template {id}");
                return StatusCode(500, new { error = "An error occurred while updating the template." });
            }
        }

        // DELETE: api/Template/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            try
            {
                var template = await _context.Templates.FindAsync(id);
                if (template == null)
                {
                    return NotFound();
                }

                _context.Templates.Remove(template);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting template {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the template." });
            }
        }

        [HttpGet("Categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.Templates
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { error = "An error occurred while retrieving categories." });
            }
        }

        // POST: api/Template/Categories
        [HttpPost("Categories")]
        public async Task<ActionResult<string>> AddCategory(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return BadRequest(new { error = "Category name cannot be empty." });
                }
                
                // Check if category already exists
                var categoryExists = await _context.Templates
                    .AnyAsync(t => t.Category == categoryName);
                    
                if (!categoryExists)
                {
                    // Get user info from claims
                    string username = User.Identity?.Name ?? "system";
                    string userCode = User.Claims.FirstOrDefault(c => c.Type == "UserCode")?.Value ?? "";
                    
                    _logger.LogInformation($"Adding category with user: {username}, code: {userCode}");
                    
                    // Create a basic template with this category to ensure it exists
                    var template = new Template
                    {
                        Name = categoryName + " Template",
                        Category = categoryName,
                        Icon = "tag",
                        BgColor = "#f0f9ff",
                        IconColor = "primary-500",
                        CreatedBy = username,
                        CreatedByID = userCode,
                        UpdatedBy = username,
                        UpdatedByID = userCode,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        FieldsJson = "[]"
                    };
                    
                    _context.Templates.Add(template);
                    await _context.SaveChangesAsync();
                }
                
                return Ok(categoryName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { error = "An error occurred while creating the category." });
            }
        }
    }
}