// Path: Controllers/CardController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using CardTagManager.Data;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CardTagManager.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardController> _logger;
        private readonly FileUploadService _fileUploadService;
        private readonly QrCodeService _qrCodeService;

        public CardController(
            ApplicationDbContext context,
            ILogger<CardController> logger,
            FileUploadService fileUploadService,
            QrCodeService qrCodeService)
        {
            _context = context;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _qrCodeService = qrCodeService;
        }

        // GET: Card
        public async Task<IActionResult> Index()
        {
            var cards = await _context.Cards
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
                
            return View(cards);
        }

        // GET: Card/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Card/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card)
        {
            try {
                _logger.LogInformation($"Create POST called for product: {card.ProductName}");
                
                // Remove validation errors for fields we'll handle ourselves
                if (ModelState.ContainsKey("CreatedBy"))
                    ModelState.Remove("CreatedBy");
                
                if (ModelState.ContainsKey("ImagePath"))
                    ModelState.Remove("ImagePath");
                    
                if (ModelState.ContainsKey("ImageFile"))
                    ModelState.Remove("ImageFile");
                
                if (ModelState.IsValid)
                {
                    // Set creator info
                    card.CreatedBy = User.Identity?.Name ?? "anonymous";
                    card.UpdatedBy = card.CreatedBy;
                    
                    // Get User_Code from claims if available
                    var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                    if (userCodeClaim != null)
                    {
                        card.CreatedByID = userCodeClaim.Value;
                        card.UpdatedByID = userCodeClaim.Value;
                        _logger.LogInformation($"Setting User_Code from claims: {card.CreatedByID}");
                    }
                    else
                    {
                        // Try to get it from LDAP service if available
                        try
                        {
                            var username = User.Identity?.Name;
                            if (!string.IsNullOrEmpty(username) && username != "anonymous")
                            {
                                var ldapService = HttpContext.RequestServices.GetService<LdapAuthenticationService>();
                                if (ldapService != null)
                                {
                                    var (_, userInfo) = ldapService.ValidateCredentials(username, null);
                                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.UserCode))
                                    {
                                        card.CreatedByID = userInfo.UserCode;
                                        card.UpdatedByID = userInfo.UserCode;
                                        _logger.LogInformation($"Retrieved User_Code from LDAP: {card.CreatedByID}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to retrieve User_Code from LDAP: {ex.Message}");
                        }
                    }
                    
                    // Handle file upload if provided
                    if (card.ImageFile != null && card.ImageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _fileUploadService.UploadFile(card.ImageFile);
                            if (uploadResult.IsSuccess)
                            {
                                card.ImagePath = uploadResult.FileUrl;
                                _logger.LogInformation($"Image uploaded: {card.ImagePath}");
                            }
                            else
                            {
                                _logger.LogWarning($"Image upload failed: {uploadResult.ErrorMessage}");
                                // Continue without image rather than failing
                                ModelState.AddModelError("ImageFile", $"Upload failed: {uploadResult.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image");
                            // Continue without image rather than failing
                            ModelState.AddModelError("ImageFile", $"Upload error: {ex.Message}");
                        }
                    }

                    // Validate and sanitize CustomFieldsData
                    if (string.IsNullOrEmpty(card.CustomFieldsData))
                    {
                        card.CustomFieldsData = "{}";
                    }
                    else
                    {
                        try
                        {
                            // Validate JSON by parsing and re-serializing
                            // This ensures proper JSON structure and fixes any malformed input
                            var customFields = JsonSerializer.Deserialize<Dictionary<string, string>>(card.CustomFieldsData);
                            
                            // Log the field names for debugging
                            _logger.LogInformation($"Custom fields: {string.Join(", ", customFields.Keys)}");
                            
                            // Re-serialize with sanitized data
                            card.CustomFieldsData = JsonSerializer.Serialize(customFields);
                        }
                        catch (Exception ex)
                        {
                            // If invalid JSON, set to empty object
                            card.CustomFieldsData = "{}";
                            _logger.LogWarning($"Invalid CustomFieldsData JSON - reset to empty object. Error: {ex.Message}");
                        }
                    }

                    // Set timestamps
                    card.CreatedAt = DateTime.Now;
                    card.UpdatedAt = DateTime.Now;

                    // Add to database
                    _context.Cards.Add(card);
                    
                    try {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Product created successfully, ID: {card.Id}");
                        
                        // Set success message for UI
                        TempData["SuccessMessage"] = $"Product '{card.ProductName}' created successfully.";
                        
                        // Redirect to the detail page
                        return RedirectToAction(nameof(Details), new { id = card.Id });
                    } catch (Exception dbEx) {
                        _logger.LogError(dbEx, "Database save error");
                        ModelState.AddModelError("", $"Database error: {dbEx.Message}");
                        return View(card);
                    }
                }
                else
                {
                    _logger.LogWarning("Model validation failed");
                    foreach (var state in ModelState)
                    {
                        if (state.Value.Errors.Count > 0)
                        {
                            _logger.LogWarning($"Field: {state.Key}, Errors: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating card");
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            // If validation fails, redisplay form
            return View(card);
        }

        // GET: Card/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Get base URL from the current request
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            // Generate QR code for display with base URL
            string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card, baseUrl);
            ViewBag.QrCodeImage = qrCodeImageData;

            return View(card);
        }
                
        // This method is kept for backwards compatibility if there are existing links
        // GET: Card/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Card/ScanShow/5
        public async Task<IActionResult> ScanShow(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Record this scan event
            var scanResult = new ScanResult
            {
                CardId = card.Id,
                ScanTime = DateTime.Now,
                DeviceInfo = Request.Headers["User-Agent"].ToString(),
                Location = Request.Headers["Referer"].ToString() ?? "Direct Access",
                Result = "Success",
                ScannedBy = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Anonymous",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            };
            
            // Extract additional useful information if possible
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                scanResult.IpAddress = Request.Headers["X-Forwarded-For"].ToString();
            }
            
            // Try to extract more meaningful location if available
            if (Request.Headers.ContainsKey("Sec-Ch-Ua-Platform"))
            {
                scanResult.DeviceInfo += " | " + Request.Headers["Sec-Ch-Ua-Platform"].ToString();
            }
            
            _context.ScanResults.Add(scanResult);
            await _context.SaveChangesAsync();

            // Generate QR code for display
            string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card);
            ViewBag.QrCodeImage = qrCodeImageData;

            return View(card);
        }

        // GET: Card/QrCode/5
        public async Task<IActionResult> QrCode(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Generate QR code for display
            string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card);
            ViewBag.QrCodeImage = qrCodeImageData;

            return View(card);
        }

        // GET: Card/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Get card history for display
            var history = await _context.CardHistories
                .Where(h => h.CardId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(10) // Get the most recent 10 changes
                .ToListAsync();
                
            ViewBag.History = history;
            
            // Generate QR code for display
            string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card);
            ViewBag.QrCodeImage = qrCodeImageData;
            
            return View(card);
        }

        // POST: Card/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Card card)
        {
            _logger.LogInformation($"Edit POST called for product ID: {id}, Name: {card.ProductName}");
            
            if (id != card.Id)
            {
                return NotFound();
            }

            // Explicitly remove validation for ImagePath and ImageFile to prevent ModelState validation errors
            ModelState.Remove("ImagePath");
            ModelState.Remove("ImageFile");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original card from the database to compare changes
                    var originalCard = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    if (originalCard == null)
                    {
                        return NotFound();
                    }
                    
                    // Preserve creation information
                    card.CreatedAt = originalCard.CreatedAt;
                    card.CreatedBy = originalCard.CreatedBy;
                    card.CreatedByID = originalCard.CreatedByID;
                    
                    // Set updater information
                    card.UpdatedBy = User.Identity?.Name ?? "system";
                    card.UpdatedAt = DateTime.Now;
                    
                    // Get User_Code from claims if available
                    var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                    if (userCodeClaim != null)
                    {
                        card.UpdatedByID = userCodeClaim.Value;
                        _logger.LogInformation($"Setting UpdatedByID from claims: {card.UpdatedByID}");
                    }
                    else
                    {
                        // If no User_Code in claims, keep original UpdatedByID
                        card.UpdatedByID = originalCard.UpdatedByID;
                    }
                    
                    // Handle image update if a new file is provided
                    if (card.ImageFile != null && card.ImageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _fileUploadService.UploadFile(card.ImageFile);
                            if (uploadResult.IsSuccess)
                            {
                                // Record the image change
                                if (originalCard.ImagePath != uploadResult.FileUrl)
                                {
                                    var imageHistory = new CardHistory
                                    {
                                        CardId = card.Id,
                                        FieldName = "Image",
                                        OldValue = originalCard.ImagePath ?? "None",
                                        NewValue = "Updated Image",
                                        ChangedAt = DateTime.Now,
                                        ChangedBy = User.Identity?.Name ?? "system"
                                    };
                                    _context.CardHistories.Add(imageHistory);
                                }
                                
                                card.ImagePath = uploadResult.FileUrl;
                            }
                            else
                            {
                                ModelState.AddModelError("ImageFile", $"Failed to upload image: {uploadResult.ErrorMessage}");
                                
                                // Load card history for re-displaying the edit form
                                var history = await _context.CardHistories
                                    .Where(h => h.CardId == id)
                                    .OrderByDescending(h => h.ChangedAt)
                                    .Take(10)
                                    .ToListAsync();
                                    
                                ViewBag.History = history;
                                return View(card);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image during edit");
                            ModelState.AddModelError("ImageFile", $"Upload error: {ex.Message}");
                            
                            // Load card history for re-displaying the edit form
                            var history = await _context.CardHistories
                                .Where(h => h.CardId == id)
                                .OrderByDescending(h => h.ChangedAt)
                                .Take(10)
                                .ToListAsync();
                                    
                            ViewBag.History = history;
                            return View(card);
                        }
                    }
                    else
                    {
                        // Critical fix: Make sure we preserve the original image path when no new image uploaded
                        _logger.LogInformation($"No new image uploaded. Preserving original image path: {originalCard.ImagePath}");
                        card.ImagePath = originalCard.ImagePath;
                    }

                    // Validate and sanitize CustomFieldsData
                    if (string.IsNullOrEmpty(card.CustomFieldsData))
                    {
                        card.CustomFieldsData = "{}";
                    }
                    else
                    {
                        try
                        {
                            // Parse and re-serialize to ensure valid JSON structure
                            var customFields = JsonSerializer.Deserialize<Dictionary<string, string>>(card.CustomFieldsData);
                            card.CustomFieldsData = JsonSerializer.Serialize(customFields);
                            
                            // Track custom fields changes if they differ from original
                            if (originalCard.CustomFieldsData != card.CustomFieldsData)
                            {
                                var fieldsHistory = new CardHistory
                                {
                                    CardId = card.Id,
                                    FieldName = "Custom Fields",
                                    OldValue = "Previous Fields Data",
                                    NewValue = "Updated Fields Data",
                                    ChangedAt = DateTime.Now,
                                    ChangedBy = User.Identity?.Name ?? "system"
                                };
                                _context.CardHistories.Add(fieldsHistory);
                            }
                        }
                        catch (Exception ex)
                        {
                            // If invalid JSON, preserve original values
                            card.CustomFieldsData = originalCard.CustomFieldsData;
                            _logger.LogWarning($"Invalid CustomFieldsData JSON during edit - kept original. Error: {ex.Message}");
                        }
                    }

                    // Track changes for history
                    await TrackCardChanges(originalCard, card);

                    // Critical fix: Ensure proper entity tracking
                    _context.Entry(originalCard).State = EntityState.Detached;
                    _context.Update(card);
                    await _context.SaveChangesAsync();
                    
                    // Generate updated QR code
                    await _qrCodeService.GenerateQrCodeImage(card);
                    
                    TempData["SuccessMessage"] = $"Product '{card.ProductName}' updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = card.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CardExists(card.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating card {id}");
                    ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
                    
                    // Load card history for re-displaying the edit form
                    var history = await _context.CardHistories
                        .Where(h => h.CardId == id)
                        .OrderByDescending(h => h.ChangedAt)
                        .Take(10)
                        .ToListAsync();
                        
                    ViewBag.History = history;
                    return View(card);
                }
            }

            // If model state is invalid, reload history and redisplay form
            var cardHistory = await _context.CardHistories
                .Where(h => h.CardId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(10)
                .ToListAsync();
                
            ViewBag.History = cardHistory;
            
            return View(card);
        }
        
        // GET: Card/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Delete GET request for product ID: {id}");
            
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                _logger.LogWarning($"Delete attempt for non-existent product ID: {id}");
                return NotFound();
            }

            try
            {
                // Generate QR code for display
                string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card);
                ViewBag.QrCodeImage = qrCodeImageData;
                
                // Get related data for context
                var recentDeletions = await _context.CardHistories
                    .Where(h => h.FieldName == "Status" && h.NewValue == "Deleted")
                    .OrderByDescending(h => h.ChangedAt)
                    .Take(3)
                    .ToListAsync();
                
                ViewBag.RecentDeletions = recentDeletions;
                
                // Check if this product has related items
                var hasRelatedItems = await _context.MaintenanceReminders
                    .AnyAsync(m => m.CardId == id);
                
                ViewBag.HasRelatedItems = hasRelatedItems;
                
                return View(card);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error preparing delete view for product ID: {id}");
                TempData["ErrorMessage"] = "An error occurred while preparing the deletion page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Card/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation($"Delete POST request for product ID: {id}");
            
            try
            {
                var card = await _context.Cards.FindAsync(id);
                if (card == null)
                {
                    _logger.LogWarning($"Delete confirmation for non-existent product ID: {id}");
                    return NotFound();
                }

                // Store product information for the success message
                string productName = card.ProductName;
                
                // Track deletion in history
                var deletionHistory = new CardHistory
                {
                    CardId = id,
                    FieldName = "Status",
                    OldValue = "Active",
                    NewValue = "Deleted",
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                };
                
                // Add User_Code to history if available
                var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                if (userCodeClaim != null)
                {
                    deletionHistory.FieldName = $"Status (ID: {userCodeClaim.Value})";
                }
                
                _context.CardHistories.Add(deletionHistory);
                
                // Delete the associated image file if exists
                if (!string.IsNullOrEmpty(card.ImagePath))
                {
                    try
                    {
                        await _fileUploadService.DeleteFile(card.ImagePath);
                        _logger.LogInformation($"Successfully deleted image file for product ID: {id}");
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with deletion
                        _logger.LogWarning(ex, $"Failed to delete image file for product ID: {id}");
                    }
                }
                
                // Delete related maintenance reminders
                var reminders = await _context.MaintenanceReminders
                    .Where(m => m.CardId == id)
                    .ToListAsync();
                
                if (reminders.Any())
                {
                    _context.MaintenanceReminders.RemoveRange(reminders);
                    _logger.LogInformation($"Deleted {reminders.Count} related maintenance reminders for product ID: {id}");
                }
                
                // Delete related documents
                var documents = await _context.CardDocuments
                    .Where(d => d.CardId == id)
                    .ToListAsync();
                
                if (documents.Any())
                {
                    _context.CardDocuments.RemoveRange(documents);
                    _logger.LogInformation($"Deleted {documents.Count} related documents for product ID: {id}");
                }
                
                // Delete related scan results
                var scanResults = await _context.ScanResults
                    .Where(s => s.CardId == id)
                    .ToListAsync();
                
                if (scanResults.Any())
                {
                    _context.ScanResults.RemoveRange(scanResults);
                    _logger.LogInformation($"Deleted {scanResults.Count} related scan results for product ID: {id}");
                }
                
                // Delete the card
                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Successfully deleted product ID: {id}, Name: {productName}");
                
                // Add success message for the index page
                TempData["SuccessMessage"] = $"Product '{productName}' has been permanently deleted.";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product ID: {id}");
                TempData["ErrorMessage"] = "An error occurred while deleting the product. Please try again or contact support.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // GET: Card/ScanResult
        public async Task<IActionResult> ScanResult()
        {
            try
            {
                // Get real scan results from the database
                var scanResults = await _context.ScanResults
                    .Include(s => s.Card)
                    .OrderByDescending(s => s.ScanTime)
                    .Select(s => new ScanResultViewModel
                    {
                        Id = s.Id,
                        CardId = s.CardId,
                        CardName = s.Card.ProductName,
                        ScanTime = s.ScanTime,
                        DeviceInfo = s.DeviceInfo,
                        Location = s.Location,
                        ScanResult = s.Result
                    })
                    .Take(100) // Limit to recent 100 for performance
                    .ToListAsync();
                
                return View(scanResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan results");
                
                // Create an empty list as fallback
                return View(new List<ScanResultViewModel>());
            }
        }

        // API endpoint to get scan results
        [HttpGet("api/Card/GetScanResults")]
        public async Task<IActionResult> GetScanResults()
        {
            try
            {
                var scanResults = await _context.ScanResults
                    .Include(s => s.Card)
                    .OrderByDescending(s => s.ScanTime)
                    .Select(s => new ScanResultViewModel
                    {
                        Id = s.Id,
                        CardId = s.CardId,
                        CardName = s.Card.ProductName,
                        ScanTime = s.ScanTime,
                        DeviceInfo = s.DeviceInfo,
                        Location = s.Location,
                        ScanResult = s.Result
                    })
                    .Take(100) // Limit to recent 100 for performance
                    .ToListAsync();
                    
                return Ok(scanResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan results");
                return StatusCode(500, new { error = "An error occurred while retrieving scan results." });
            }
        }

        // POST: DeleteScanResult endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteScanResult(int id)
        {
            try
            {
                var scanResult = await _context.ScanResults.FindAsync(id);
                if (scanResult == null)
                {
                    return Json(new { success = false, error = "Scan result not found" });
                }
                
                _context.ScanResults.Remove(scanResult);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting scan result {id}");
                return Json(new { success = false, error = "An error occurred while deleting the scan result." });
            }
        }
        
        // GET: Card/GetCardHistory/5
        [HttpGet]
        public async Task<IActionResult> GetCardHistory(int id)
        {
            // Get card history for the specified card
            var history = await _context.CardHistories
                .Where(h => h.CardId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(20) // Limit to recent 20 records
                .ToListAsync();
                
            return Json(history);
        }

        // Helper method to check if a card exists
        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }
        
        // GET: Card/GetCardIssues/5 
        [HttpGet]
        public async Task<IActionResult> GetCardIssues(int id)
        {
            var issues = await _context.IssueReports
                .Where(i => i.CardId == id)
                .OrderByDescending(i => i.ReportDate)
                .ToListAsync();
                
            return Json(issues);
        }
        
        // POST: Card/ReportIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportIssue([FromBody] IssueReport issue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // Validate the card exists
                var card = await _context.Cards.FindAsync(issue.CardId);
                if (card == null)
                {
                    return NotFound(new { error = "Product not found" });
                }
                
                // Set default values
                issue.CreatedAt = DateTime.Now;
                issue.Status = "Open";
                
                // Add User_Code if available
                var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
                if (userCodeClaim != null)
                {
                    issue.ReporterName += $" (ID: {userCodeClaim.Value})";
                }
                
                _context.IssueReports.Add(issue);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, issueId = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue report");
                return StatusCode(500, new { error = "An error occurred while creating the issue report" });
            }
        }
        
        // GET: Card/GetCardDocuments/5
        [HttpGet]
        public async Task<IActionResult> GetCardDocuments(int id)
        {
            var documents = await _context.CardDocuments
                .Where(d => d.CardId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
                
            return Json(documents);
        }
        
        // GET: Card/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.CardDocuments.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }
            
            // In a real implementation, you would retrieve the file from storage
            // For now, redirect to the file URL
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                return Redirect(document.FilePath);
            }
            else
            {
                return NotFound("Document file not found");
            }
        }
        
        // GET: Card/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Generate QR code for printing
            string qrCodeImageData = await _qrCodeService.GenerateQrCodeImage(card);
            ViewBag.QrCodeImage = qrCodeImageData;

            return View(card);
        }

        // GET: Card/PrintAll
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();
            
            // Generate QR codes for all cards
            var cardIds = cards.Select(c => c.Id).ToList();
            ViewBag.QrCodeImages = new Dictionary<int, string>();
            
            foreach (var card in cards)
            {
                ViewBag.QrCodeImages[card.Id] = await _qrCodeService.GenerateQrCodeImage(card);
            }
            
            return View(cards);
        }
        
        // GET: Card/DownloadQrCode/5
        public async Task<IActionResult> DownloadQrCode(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Generate QR code image for download
            var qrCodeBytes = await _qrCodeService.GenerateQrCodeBytes(card);
            
            if (qrCodeBytes == null || qrCodeBytes.Length == 0)
            {
                return NotFound("Could not generate QR code");
            }
            
            // Create a safe filename based on the product name
            var filename = card.ProductName.Replace(" ", "_");
            filename = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
            
            return File(qrCodeBytes, "image/png", $"{filename}_QRCode.png");
        }

        // API endpoint for getting all issues
        [HttpGet]
        [Route("api/[controller]/GetAllIssues")] // Explicit routing definition
        public async Task<IActionResult> GetAllIssues()
        {
            try
            {
                var issues = await _context.IssueReports
                    .Include(i => i.Card)
                    .OrderByDescending(i => i.ReportDate)
                    .Select(i => new {
                        id = i.Id,
                        cardId = i.CardId,
                        cardName = i.Card.ProductName,
                        status = i.Status,
                        priority = i.Priority,
                        issueType = i.IssueType,
                        description = i.Description,
                        reportDate = i.ReportDate,
                        reporterName = i.ReporterName,
                        reporterEmail = i.ReporterEmail,
                        resolution = i.Resolution,
                        resolvedAt = i.ResolvedAt
                    })
                    .Take(100) // Limit to recent 100 for performance
                    .ToListAsync();
                    
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all issues");
                return StatusCode(500, new { error = "An error occurred while retrieving issues." });
            }
        }

        // POST: UpdateIssueStatus endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueStatus([FromBody] IssueStatusUpdateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, error = "Invalid model state" });
                }
                
                var issue = await _context.IssueReports.FindAsync(model.Id);
                if (issue == null)
                {
                    return Json(new { success = false, error = "Issue not found" });
                }
                
                issue.Status = model.Status;
                
                if (model.Status == "Resolved" || model.Status == "Closed")
                {
                    issue.Resolution = model.Resolution ?? string.Empty;
                    issue.ResolvedAt = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating issue status");
                return Json(new { success = false, error = "An error occurred while updating the issue status." });
            }
        }
        
        // Helper method to track card changes
        private async Task TrackCardChanges(Card originalCard, Card updatedCard)
        {
            var changedProperties = new List<CardHistory>();
            
            // Compare basic properties
            if (originalCard.ProductName != updatedCard.ProductName)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Product Name",
                    OldValue = originalCard.ProductName,
                    NewValue = updatedCard.ProductName,
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.Category != updatedCard.Category)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Category",
                    OldValue = originalCard.Category,
                    NewValue = updatedCard.Category,
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.Location != updatedCard.Location)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Location",
                    OldValue = originalCard.Location ?? "None",
                    NewValue = updatedCard.Location ?? "None",
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.MaintenanceInfo != updatedCard.MaintenanceInfo)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Maintenance Info",
                    OldValue = originalCard.MaintenanceInfo ?? "None",
                    NewValue = updatedCard.MaintenanceInfo ?? "None",
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.ManufactureDate.Date != updatedCard.ManufactureDate.Date)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Manufacture Date",
                    OldValue = originalCard.ManufactureDate.ToShortDateString(),
                    NewValue = updatedCard.ManufactureDate.ToShortDateString(),
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.PurchaseDate.Date != updatedCard.PurchaseDate.Date)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Purchase Date",
                    OldValue = originalCard.PurchaseDate.ToShortDateString(),
                    NewValue = updatedCard.PurchaseDate.ToShortDateString(),
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.WarrantyExpiration.Date != updatedCard.WarrantyExpiration.Date)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Warranty Expiration",
                    OldValue = originalCard.WarrantyExpiration.ToShortDateString(),
                    NewValue = updatedCard.WarrantyExpiration.ToShortDateString(),
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            // Compare appearance settings
            if (originalCard.BackgroundColor != updatedCard.BackgroundColor)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Background Color",
                    OldValue = originalCard.BackgroundColor,
                    NewValue = updatedCard.BackgroundColor,
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.TextColor != updatedCard.TextColor)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Text Color",
                    OldValue = originalCard.TextColor,
                    NewValue = updatedCard.TextColor,
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.AccentColor != updatedCard.AccentColor)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "Accent Color",
                    OldValue = originalCard.AccentColor,
                    NewValue = updatedCard.AccentColor,
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            // Compare QR code colors if they exist
            if (originalCard.QrFgColor != updatedCard.QrFgColor)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "QR Foreground Color",
                    OldValue = originalCard.QrFgColor ?? "#000000",
                    NewValue = updatedCard.QrFgColor ?? "#000000",
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            if (originalCard.QrBgColor != updatedCard.QrBgColor)
            {
                changedProperties.Add(new CardHistory
                {
                    CardId = originalCard.Id,
                    FieldName = "QR Background Color",
                    OldValue = originalCard.QrBgColor ?? "#FFFFFF",
                    NewValue = updatedCard.QrBgColor ?? "#FFFFFF",
                    ChangedAt = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "system"
                });
            }
            
            // Add User_Code to each history entry if available
            var userCodeClaim = User.Claims.FirstOrDefault(c => c.Type == "User_Code");
            if (userCodeClaim != null)
            {
                string userCode = userCodeClaim.Value;
                foreach (var history in changedProperties)
                {
                    history.ChangedBy = $"{history.ChangedBy} (ID: {userCode})";
                }
            }
            
            // Add all changes to the database if there are any
            if (changedProperties.Count > 0)
            {
                _context.CardHistories.AddRange(changedProperties);
                await _context.SaveChangesAsync();
            }
        }

        // API endpoint for scan analytics
        [HttpGet("api/[controller]/GetScanAnalytics")]
        public async Task<IActionResult> GetScanAnalytics(string period = "week")
        {
            try
            {
                // Determine date range based on period
                var endDate = DateTime.Now;
                var startDate = period.ToLower() switch
                {
                    "week" => endDate.AddDays(-7),
                    "month" => endDate.AddDays(-30),
                    "year" => endDate.AddYears(-1),
                    _ => endDate.AddDays(-7)
                };
                
                // Query scan results from database
                var scanData = await _context.ScanResults
                    .Where(s => s.ScanTime >= startDate && s.ScanTime <= endDate)
                    .GroupBy(s => period.ToLower() switch
                    {
                        "week" => s.ScanTime.Date,
                        "month" => s.ScanTime.Date,
                        "year" => new DateTime(s.ScanTime.Year, s.ScanTime.Month, 1),
                        _ => s.ScanTime.Date
                    })
                    .Select(g => new
                    {
                        date = g.Key,
                        count = g.Count()
                    })
                    .OrderBy(g => g.date)
                    .ToListAsync();
                
                // For week/month with sparse data, fill in missing dates
                if (period.ToLower() == "week" || period.ToLower() == "month")
                {
                    var filledData = new List<object>();
                    var current = startDate.Date;
                    
                    while (current <= endDate.Date)
                    {
                        var existingData = scanData.FirstOrDefault(d => ((DateTime)d.date).Date == current);
                        filledData.Add(new
                        {
                            date = current,
                            count = existingData != null ? (int)existingData.count : 0
                        });
                        
                        current = current.AddDays(1);
                    }
                    
                    return Ok(filledData);
                }
                // For year, fill in missing months
                else if (period.ToLower() == "year")
                {
                    var filledData = new List<object>();
                    var current = new DateTime(startDate.Year, startDate.Month, 1);
                    
                    while (current <= endDate)
                    {
                        var existingData = scanData.FirstOrDefault(d => 
                            ((DateTime)d.date).Year == current.Year && 
                            ((DateTime)d.date).Month == current.Month);
                        
                        filledData.Add(new
                        {
                            date = current,
                            count = existingData != null ? (int)existingData.count : 0
                        });
                        
                        current = current.AddMonths(1);
                    }
                    
                    return Ok(filledData);
                }
                
                return Ok(scanData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scan analytics");
                return StatusCode(500, new { error = "An error occurred while retrieving scan analytics." });
            }
        }

        // API endpoint for issue analytics
        [HttpGet("api/[controller]/GetIssueAnalytics")]
        public async Task<IActionResult> GetIssueAnalytics()
        {
            try
            {
                // Get issues by status
                var issuesByStatus = await _context.IssueReports
                    .GroupBy(i => i.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToDictionaryAsync(g => g.status, g => g.count);
                
                // Get issues by priority
                var issuesByPriority = await _context.IssueReports
                    .GroupBy(i => i.Priority)
                    .Select(g => new { priority = g.Key, count = g.Count() })
                    .ToDictionaryAsync(g => g.priority, g => g.count);
                
                // Combine the results
                var result = new
                {
                    byStatus = issuesByStatus,
                    byPriority = issuesByPriority
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issue analytics");
                return StatusCode(500, new { error = "An error occurred while retrieving issue analytics." });
            }
        }

        // API endpoint for top products with issues
        [HttpGet("api/[controller]/GetTopProductsWithIssues")]
        public async Task<IActionResult> GetTopProductsWithIssues()
        {
            try
            {
                // Group issues by card and calculate metrics
                var productIssues = await _context.IssueReports
                    .Include(i => i.Card)
                    .GroupBy(i => i.CardId)
                    .Select(g => new
                    {
                        cardId = g.Key,
                        productName = g.First().Card.ProductName,
                        totalIssues = g.Count(),
                        openIssues = g.Count(i => i.Status == "Open" || i.Status == "In Progress"),
                        // Most common issue type
                        commonIssue = g.GroupBy(i => i.IssueType)
                                    .OrderByDescending(ig => ig.Count())
                                    .Select(ig => ig.Key)
                                    .FirstOrDefault() ?? "Unknown",
                        // Most recent issue report date
                        lastReported = g.Max(i => i.ReportDate)
                    })
                    .OrderByDescending(p => p.totalIssues)
                    .Take(10) // Limit to top 10
                    .ToListAsync();
                
                return Ok(productIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with issues");
                return StatusCode(500, new { error = "An error occurred while retrieving products with issues." });
            }
        } 
    }
}