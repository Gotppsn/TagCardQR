// Controllers/TemplateController.cs
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

                template.CreatedBy = User.Identity.Name;
                template.CreatedAt = DateTime.Now;
                template.UpdatedAt = DateTime.Now;

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

                existingTemplate.Name = template.Name;
                existingTemplate.Category = template.Category;
                existingTemplate.Icon = template.Icon;
                existingTemplate.BgColor = template.BgColor;
                existingTemplate.FieldsJson = template.FieldsJson;
                existingTemplate.UpdatedAt = DateTime.Now;

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
    }
}