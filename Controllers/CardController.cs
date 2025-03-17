// Path: Controllers/CardController.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using CardTagManager.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CardTagManager.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QrCodeService _qrCodeService;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<CardController> _logger;

        public CardController(
            ApplicationDbContext context, 
            QrCodeService qrCodeService, 
            FileUploadService fileUploadService,
            ILogger<CardController> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // GET: Card - Main card listing page
        public async Task<IActionResult> Index()
        {
            try
            {
                var cards = await _context.Cards.ToListAsync();
                return View(cards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cards");
                TempData["ErrorMessage"] = "Error loading products: " + ex.Message;
                return View(new List<Card>());
            }
        }

        // GET: Card/Details/5 - Shows detailed card information
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var card = await _context.Cards.FindAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                // Get QR colors from TempData or use defaults
                string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
                string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
                
                // Preserve QR colors in TempData for subsequent requests
                TempData.Keep("QrFgColor");
                TempData.Keep("QrBgColor");

                // Generate QR code for card details view
                string qrCodeData = GenerateCardQrData(card);
                ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

                return View(card);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving card details for ID: {id}");
                TempData["ErrorMessage"] = "Error loading product details: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Card/Create - Shows card creation form
        public IActionResult Create()
        {
            var card = new Card();
            
            // Pre-fill user information from claims
            if (User.Identity.IsAuthenticated)
            {
                card.Username = User.FindFirstValue("Username") ?? User.Identity.Name;
                card.Department = User.FindFirstValue("Department") ?? "";
                card.Email = User.FindFirstValue("Email") ?? "";
                card.UserFullName = User.FindFirstValue("FullName") ?? "";
                card.PlantName = User.FindFirstValue("PlantName") ?? "";
            }
            
            return View(card);
        }

        // POST: Card/Create - Handles form submission for new card
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card, IFormFile ImageFile, string qrFgColor, string qrBgColor)
        {
            try
            {
                _logger.LogInformation($"Creating card: {card.ProductName}, Category: {card.Category}");
                
                // Set user information from claims
                if (User.Identity.IsAuthenticated)
                {
                    card.Username = User.FindFirstValue("Username") ?? User.Identity.Name;
                    card.Department = User.FindFirstValue("Department") ?? "";
                    card.Email = User.FindFirstValue("Email") ?? "";
                    card.UserFullName = User.FindFirstValue("FullName") ?? "";
                    card.PlantName = User.FindFirstValue("PlantName") ?? "";
                }
                
                if (ModelState.IsValid)
                {
                    // Handle image upload if a file was provided
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        try
                        {
                            _logger.LogInformation($"Uploading image: {ImageFile.FileName}, Size: {ImageFile.Length}");
                            var fileResponse = await _fileUploadService.UploadFile(ImageFile);
                            if (fileResponse.IsSuccess)
                            {
                                // Save the image URL to the card
                                card.ImagePath = fileResponse.FileUrl;
                                _logger.LogInformation($"Image uploaded successfully: {fileResponse.FileUrl}");
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to upload image: {fileResponse.ErrorMessage}");
                                ModelState.AddModelError("ImageFile", "Failed to upload image: " + fileResponse.ErrorMessage);
                                return View(card);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading image");
                            ModelState.AddModelError("ImageFile", "Error uploading image: " + ex.Message);
                            return View(card);
                        }
                    }

                    // Set creation timestamps explicitly
                    card.CreatedAt = DateTime.Now;
                    card.UpdatedAt = DateTime.Now;
                    
                    // Store QR color preferences in TempData for the session
                    if (!string.IsNullOrEmpty(qrFgColor))
                        TempData["QrFgColor"] = qrFgColor;
                    
                    if (!string.IsNullOrEmpty(qrBgColor))
                        TempData["QrBgColor"] = qrBgColor;
                    
                    try
                    {
                        // Add the card to the context
                        _context.Cards.Add(card);
                        
                        // Save the changes to the database
                        await _context.SaveChangesAsync();
                        
                        // Add history record for creation
                        var createHistory = new CardHistory
                        {
                            CardId = card.Id,
                            FieldName = "Creation",
                            OldValue = "",
                            NewValue = "Initial product creation",
                            ChangedAt = DateTime.Now,
                            ChangedBy = User.Identity.Name
                        };
                        
                        _context.CardHistories.Add(createHistory);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Card saved successfully with ID: {card.Id}");
                        TempData["SuccessMessage"] = $"Product '{card.ProductName}' created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Database error when saving card");
                        ModelState.AddModelError(string.Empty, "Database error: " + (dbEx.InnerException?.Message ?? dbEx.Message));
                    }
                }
                else
                {
                    // Log validation errors
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning($"Model validation failed: {errors}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create action");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
            }
            
            // If we got this far, something failed, redisplay form
            return View(card);
        }

        // GET: Card/Edit/5 - Shows card editing form
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");
            
            // Get edit history
            var history = await _context.CardHistories
                .Where(ch => ch.CardId == id)
                .OrderByDescending(ch => ch.ChangedAt)
                .Take(5)
                .ToListAsync();
                
            ViewBag.History = history;
            
            // Generate QR code for preview with custom colors
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);
            
            return View(card);
        }

        // POST: Card/Edit/5 - Handles form submission for card updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Card card, IFormFile ImageFile, string qrFgColor, string qrBgColor)
        {
            if (id != card.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing card
                    var existingCard = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    if (existingCard == null)
                    {
                        return NotFound();
                    }
                    
                    // Keep the user information from the database
                    card.Username = existingCard.Username;
                    card.Department = existingCard.Department;
                    card.Email = existingCard.Email;
                    card.UserFullName = existingCard.UserFullName;
                    card.PlantName = existingCard.PlantName;
                    card.CreatedAt = existingCard.CreatedAt;
                    
                    // Handle image upload if a file was provided
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        try
                        {
                            // Delete old image if it exists
                            if (!string.IsNullOrEmpty(existingCard.ImagePath))
                            {
                                await _fileUploadService.DeleteFile(existingCard.ImagePath);
                            }
                            
                            var fileResponse = await _fileUploadService.UploadFile(ImageFile);
                            if (fileResponse.IsSuccess)
                            {
                                // Save the image URL to the card
                                card.ImagePath = fileResponse.FileUrl;
                            }
                            else
                            {
                                ModelState.AddModelError("ImageFile", "Failed to upload image: " + fileResponse.ErrorMessage);
                                return View(card);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ImageFile", "Error uploading image: " + ex.Message);
                            return View(card);
                        }
                    }
                    else
                    {
                        // Keep the existing image if no new one was uploaded
                        card.ImagePath = existingCard.ImagePath;
                    }

                    // Store QR color preferences in TempData for the session
                    if (!string.IsNullOrEmpty(qrFgColor))
                        TempData["QrFgColor"] = qrFgColor;
                    
                    if (!string.IsNullOrEmpty(qrBgColor))
                        TempData["QrBgColor"] = qrBgColor;

                    // Track changes for history
                    var changedProperties = new List<CardHistory>();
                    
                    // Define properties to track (excluding system-managed properties)
                    var propertiesToTrack = typeof(Card).GetProperties()
                        .Where(p => 
                            p.Name != "Id" && 
                            p.Name != "CreatedAt" && 
                            p.Name != "UpdatedAt" &&
                            p.Name != "Username" && 
                            p.Name != "Department" && 
                            p.Name != "Email" && 
                            p.Name != "UserFullName" && 
                            p.Name != "PlantName")
                        .ToList();

                    foreach (var prop in propertiesToTrack)
                    {
                        var oldValue = prop.GetValue(existingCard)?.ToString();
                        var newValue = prop.GetValue(card)?.ToString();
                        
                        if (oldValue != newValue)
                        {
                            changedProperties.Add(new CardHistory
                            {
                                CardId = card.Id,
                                FieldName = prop.Name,
                                OldValue = oldValue ?? "",
                                NewValue = newValue ?? "",
                                ChangedAt = DateTime.Now,
                                ChangedBy = User.Identity.Name
                            });
                        }
                    }

                    card.UpdatedAt = DateTime.Now;
                    _context.Update(card);
                    
                    // Add change history if any properties changed
                    if (changedProperties.Any())
                    {
                        await _context.CardHistories.AddRangeAsync(changedProperties);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Product '{card.ProductName}' updated successfully.";
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
                return RedirectToAction(nameof(Details), new { id = card.Id });
            }
            
            // If model validation fails, regenerate the QR code for the preview
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(
                qrCodeData, 
                qrFgColor ?? "#000000", 
                qrBgColor ?? "#FFFFFF"
            );
            
            return View(card);
        }

        // GET: Card/Delete/5 - Shows deletion confirmation
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");
            
            // Generate QR code for preview with custom colors
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            return View(card);
        }

        // POST: Card/Delete/5 - Handles card deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var card = await _context.Cards.FindAsync(id);
                if (card != null)
                {
                    // Delete associated image if exists
                    if (!string.IsNullOrEmpty(card.ImagePath))
                    {
                        try
                        {
                            await _fileUploadService.DeleteFile(card.ImagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to delete image file: {card.ImagePath}");
                        }
                    }
                    
                    // Delete history records
                    var histories = await _context.CardHistories.Where(h => h.CardId == id).ToListAsync();
                    if (histories.Any())
                    {
                        _context.CardHistories.RemoveRange(histories);
                    }
                    
                    _context.Cards.Remove(card);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Product '{card.ProductName}' deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting card with ID: {id}");
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Card/Print/5 - Generates printable card tag
        public async Task<IActionResult> Print(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");

            // Generate QR code for printable tag with custom colors
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            return View(card);
        }

        // GET: Card/PrintAll - Print all card tags
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();

            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");

            // Generate QR codes for all cards with custom colors
            var qrCodes = new Dictionary<int, string>();
            foreach (var card in cards)
            {
                string qrCodeData = GenerateCardQrData(card);
                qrCodes[card.Id] = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);
            }

            ViewBag.QrCodes = qrCodes;

            return View(cards);
        }

        // GET: Card/QrCode/5 - Dedicated QR code display for scanning
        public async Task<IActionResult> QrCode(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");

            // Generate QR code data with custom colors
            string qrCodeData = GenerateCardQrData(card);
            string qrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            ViewBag.QrCodeImage = qrCodeImage;
            ViewBag.CardName = card.ProductName;

            return View(card);
        }

        // GET: Card/ScanResult - Shows scan result demo data
        public IActionResult ScanResult()
        {
            // Demo data for testing the ScanResult view
            var demoResults = new List<ScanResultViewModel>
            {
                new ScanResultViewModel
                {
                    Id = 1,
                    CardId = 1,
                    CardName = "RustShield Pro 5000",
                    ScanTime = DateTime.Now.AddHours(-2),
                    DeviceInfo = "iPhone 14 Pro, iOS 16.5",
                    Location = "Chemical Storage Room A",
                    ScanResult = "Success"
                },
                new ScanResultViewModel
                {
                    Id = 7,
                    CardId = 1,
                    CardName = "RustShield Pro 5000",
                    ScanTime = DateTime.Now.AddDays(-2),
                    DeviceInfo = "iPhone SE, iOS 16.3",
                    Location = "Storage Area",
                    ScanResult = "Success"
                }
            };
            
            return View(demoResults);
        }
        
        // GET: Card/ScanShow/5 - Shows card info for mobile scan view
        public async Task<IActionResult> ScanShow(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");
            
            // Generate QR code for mobile view with custom colors
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            // Log scan activity for analytics (simulated)
            _logger.LogInformation($"Card {id} ({card.ProductName}) was scanned at {DateTime.Now}");

            return View(card);
        }
        
        // GET: Card/DownloadData/5 - Generate downloadable card data file
        public async Task<IActionResult> DownloadData(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Generate card data content in a structured format
            var cardData = new
            {
                ProductName = card.ProductName,
                Category = card.Category,
                Manufacturer = card.Manufacturer,
                ModelNumber = card.ModelNumber,
                SerialNumber = card.SerialNumber,
                Location = card.Location,
                MaintenanceInfo = card.MaintenanceInfo,
                ManufactureDate = card.ManufactureDate.ToString("yyyy-MM-dd"),
                PurchaseDate = card.PurchaseDate.ToString("yyyy-MM-dd"),
                WarrantyExpiration = card.WarrantyExpiration.ToString("yyyy-MM-dd"),
                CreatedBy = card.Username,
                Department = card.Department,
                LastUpdated = card.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Convert to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(cardData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Create a file name
            string fileName = $"{card.ProductName.Replace(" ", "_")}_Info.json";

            // Return the data as a downloadable file
            return File(System.Text.Encoding.UTF8.GetBytes(jsonData), "application/json", fileName);
        }
        
        // GET: Card/DownloadQrCode/5 - Download QR code as image file
        public async Task<IActionResult> DownloadQrCode(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Get QR colors from TempData or use defaults
            string qrFgColor = TempData["QrFgColor"]?.ToString() ?? "#000000";
            string qrBgColor = TempData["QrBgColor"]?.ToString() ?? "#FFFFFF";
            
            // Preserve values in TempData
            TempData.Keep("QrFgColor");
            TempData.Keep("QrBgColor");

            // Generate QR code for download with custom colors
            string qrCodeData = GenerateCardQrData(card);
            string qrCodeBase64 = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);
            
            // Convert base64 to bytes (strip data URI prefix)
            string base64Data = qrCodeBase64.Split(',')[1];
            byte[] qrBytes = Convert.FromBase64String(base64Data);

            // Return as downloadable file
            string fileName = $"{card.ProductName.Replace(" ", "_")}_QR.png";
            return File(qrBytes, "image/png", fileName);
        }
        
        // GET: Card/History/5 - View complete edit history for a card
        public async Task<IActionResult> History(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            var history = await _context.CardHistories
                .Where(h => h.CardId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
                
            ViewBag.Card = card;
            return View(history);
        }

        // Helper method to generate card data for QR code
        private string GenerateCardQrData(Card card)
        {
            // Get the base URL dynamically
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            // Create a URL to the ScanShow action
            string scanUrl = $"{baseUrl}/Card/ScanShow/{card.Id}";
            
            // Return just the URL so QR code scanners will open it
            return scanUrl;
        }

        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }
    }
}