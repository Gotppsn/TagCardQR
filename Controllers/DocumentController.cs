// Path: Controllers/DocumentController.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardTagManager.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            ApplicationDbContext context,
            FileUploadService fileUploadService,
            ILogger<DocumentController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // GET: api/Document/card/5
        [AllowAnonymous]
        [HttpGet("card/{cardId}")]
        public async Task<ActionResult<IEnumerable<CardDocument>>> GetCardDocuments(int cardId)
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

                var documents = await _context.CardDocuments
                    .Where(d => d.CardId == cardId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving documents for card {cardId}");
                return StatusCode(500, new { error = "An error occurred while retrieving documents." });
            }
        }

        // GET: api/Document/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CardDocument>> GetDocument(int id)
        {
            var document = await _context.CardDocuments.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }

        // GET: api/Document/download/5
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.CardDocuments.FindAsync(id);

                if (document == null)
                {
                    return NotFound();
                }

                // In a real implementation, you would use the document.FilePath to retrieve the file
                // For demo purposes, we'll just return a 404
                return NotFound("File download functionality not implemented yet.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading document {id}");
                return StatusCode(500, new { error = "An error occurred while downloading the document." });
            }
        }

        // POST: api/Document
        [HttpPost]
        public async Task<ActionResult<CardDocument>> UploadDocument([FromForm] CardDocument document)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (document.DocumentFile == null || document.DocumentFile.Length == 0)
            {
                return BadRequest(new { error = "No file was uploaded." });
            }

            try
            {
                // Upload file using FileUploadService
                var fileResponse = await _fileUploadService.UploadFile(document.DocumentFile, "CardDocuments");

                if (!fileResponse.IsSuccess)
                {
                    return BadRequest(new { error = fileResponse.ErrorMessage });
                }

                // Create new document record
                var newDocument = new CardDocument
                {
                    CardId = document.CardId,
                    Title = document.Title,
                    DocumentType = document.DocumentType,
                    Description = document.Description,
                    FilePath = fileResponse.FileUrl,
                    FileName = document.DocumentFile.FileName,
                    FileSize = document.DocumentFile.Length,
                    FileType = document.DocumentFile.ContentType,
                    UploadedAt = DateTime.Now,
                    UploadedBy = User.Identity.Name
                };

                _context.CardDocuments.Add(newDocument);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDocument), new { id = newDocument.Id }, newDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { error = "An error occurred while uploading the document." });
            }
        }

        // DELETE: api/Document/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var document = await _context.CardDocuments.FindAsync(id);

                if (document == null)
                {
                    return NotFound();
                }

                // Delete file using FileUploadService
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    try
                    {
                        await _fileUploadService.DeleteFile(document.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete file: {document.FilePath}");
                    }
                }

                _context.CardDocuments.Remove(document);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the document." });
            }
        }

        [HttpPost("UploadMultiple")]
        public async Task<ActionResult<CardDocument>> UploadMultiple(
            [FromForm] int cardId,
            [FromForm] string title,
            [FromForm] string documentType,
            [FromForm] string description,
            [FromForm] List<IFormFile> documentFiles)
        {
            try
            {
                // Ensure description is never null
                description = description ?? string.Empty;

                if (documentFiles == null || !documentFiles.Any())
                {
                    return BadRequest(new { error = "No files were uploaded." });
                }

                var card = await _context.Cards.FindAsync(cardId);
                if (card == null)
                {
                    return NotFound(new { error = "Card not found." });
                }

                int successCount = 0;
                List<string> errors = new List<string>();

                foreach (var file in documentFiles)
                {
                    if (file.Length == 0)
                    {
                        errors.Add($"Empty file: {file.FileName}");
                        continue;
                    }

                    try
                    {
                        // Debug logging
                        _logger.LogInformation($"Attempting to upload file: {file.FileName}, size: {file.Length}");

                        // Upload file using FileUploadService
                        var fileResponse = await _fileUploadService.UploadFile(file, "CardDocuments");

                        if (!fileResponse.IsSuccess)
                        {
                            _logger.LogError($"Upload failed for {file.FileName}: {fileResponse.ErrorMessage}");
                            errors.Add($"Failed to upload {file.FileName}: {fileResponse.ErrorMessage}");
                            continue;
                        }

                        // Generate unique title for each file if multiple files
                        string fileTitle = documentFiles.Count > 1
                            ? $"{title} - {file.FileName}"
                            : title;

                        // Create new document record
                        // Create new document record
                        var newDocument = new CardDocument
                        {
                            CardId = cardId,
                            Title = fileTitle,
                            DocumentType = documentType,
                            Description = description,
                            FilePath = fileResponse.FileUrl,
                            FileName = file.FileName, // Store original filename for display
                            FileSize = file.Length,
                            FileType = file.ContentType,
                            UploadedAt = DateTime.Now,
                            UploadedBy = User.Identity?.Name ?? "unknown"
                        };

                        _context.CardDocuments.Add(newDocument);
                        successCount++;
                        _logger.LogInformation($"Successfully added document record for {file.FileName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error uploading file: {file.FileName}");
                        errors.Add($"Error: {file.FileName} - {ex.Message}");
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Successfully saved {successCount} document records");

                    return Ok(new
                    {
                        success = true,
                        count = successCount,
                        errors = errors.Any() ? errors : null
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "All uploads failed",
                        errors = errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple documents");
                return StatusCode(500, new { error = $"An error occurred while uploading the documents: {ex.Message}" });
            }
        }
    }
}