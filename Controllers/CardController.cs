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

                // Use QR colors from the card database values
                string qrFgColor = card.QrFgColor ?? "#000000";
                string qrBgColor = card.QrBgColor ?? "#FFFFFF";
                
                // Also store in TempData for subsequent requests
                TempData["QrFgColor"] = qrFgColor;
                TempData["QrBgColor"] = qrBgColor;

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
        public async Task<IActionResult> Create(Card card, IFormFile ImageFile)
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
                
                // Verify background colors are set
                if (string.IsNullOrEmpty(card.BackgroundColor))
                    card.BackgroundColor = "#ffffff";
                
                if (string.IsNullOrEmpty(card.TextColor))
                    card.TextColor = "#000000";
                
                if (string.IsNullOrEmpty(card.AccentColor))
                    card.AccentColor = "#0284c7";
                
                // Verify QR colors are set
                if (string.IsNullOrEmpty(card.QrFgColor))
                    card.QrFgColor = "#000000";
                    
                if (string.IsNullOrEmpty(card.QrBgColor))
                    card.QrBgColor = "#FFFFFF";
                    
                // Verify layout is set
                if (string.IsNullOrEmpty(card.Layout))
                    card.Layout = "standard";
                
                // Remove specific validation errors before checking model state
                if (ModelState.ContainsKey("ImageFile"))
                    ModelState.Remove("ImageFile");
                    
                if (ModelState.ContainsKey("Email"))
                    ModelState.Remove("Email");
                
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning($"Model validation failed: {errors}");
                    return View(card);
                }

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
                TempData["QrFgColor"] = card.QrFgColor;
                TempData["QrBgColor"] = card.QrBgColor;
                
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
            
            // Use QR colors from the card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";
            
            // Also store in TempData for subsequent requests
            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;
            
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
        public async Task<IActionResult> Edit(int id, Card card, IFormFile ImageFile)
        {
            if (id != card.Id)
            {
                return NotFound();
            }

            // Remove specific validation errors before checking model state
            if (ModelState.ContainsKey("ImageFile"))
                ModelState.Remove("ImageFile");
                
            if (ModelState.ContainsKey("Email"))
                ModelState.Remove("Email");
            
            if (!ModelState.IsValid)
            {
                // Generate QR code for preview if validation fails
                string qrCodeData = GenerateCardQrData(card);
                ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(
                    qrCodeData, 
                    card.QrFgColor ?? "#000000", 
                    card.QrBgColor ?? "#FFFFFF"
                );
                
                // Get edit history for the view
                var history = await _context.CardHistories
                    .Where(ch => ch.CardId == id)
                    .OrderByDescending(ch => ch.ChangedAt)
                    .Take(5)
                    .ToListAsync();
                    
                ViewBag.History = history;
                
                return View(card);
            }

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
                
                // Verify background colors are set
                if (string.IsNullOrEmpty(card.BackgroundColor))
                    card.BackgroundColor = existingCard.BackgroundColor ?? "#ffffff";
                
                if (string.IsNullOrEmpty(card.TextColor))
                    card.TextColor = existingCard.TextColor ?? "#000000";
                
                if (string.IsNullOrEmpty(card.AccentColor))
                    card.AccentColor = existingCard.AccentColor ?? "#0284c7";
                
                // Verify QR colors are set
                if (string.IsNullOrEmpty(card.QrFgColor))
                    card.QrFgColor = existingCard.QrFgColor ?? "#000000";
                    
                if (string.IsNullOrEmpty(card.QrBgColor))
                    card.QrBgColor = existingCard.QrBgColor ?? "#FFFFFF";
                    
                // Verify layout is set
                if (string.IsNullOrEmpty(card.Layout))
                    card.Layout = existingCard.Layout ?? "standard";
                
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
                            
                            // Generate QR code for preview if error
                            string qrCodeData = GenerateCardQrData(card);
                            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(
                                qrCodeData, 
                                card.QrFgColor ?? "#000000", 
                                card.QrBgColor ?? "#FFFFFF"
                            );
                            
                            // Get edit history for the view
                            var history = await _context.CardHistories
                                .Where(ch => ch.CardId == id)
                                .OrderByDescending(ch => ch.ChangedAt)
                                .Take(5)
                                .ToListAsync();
                                
                            ViewBag.History = history;
                            
                            return View(card);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageFile", "Error uploading image: " + ex.Message);
                        
                        // Generate QR code for preview if error
                        string qrCodeData = GenerateCardQrData(card);
                        ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(
                            qrCodeData, 
                            card.QrFgColor ?? "#000000", 
                            card.QrBgColor ?? "#FFFFFF"
                        );
                        
                        // Get edit history for the view
                        var history = await _context.CardHistories
                            .Where(ch => ch.CardId == id)
                            .OrderByDescending(ch => ch.ChangedAt)
                            .Take(5)
                            .ToListAsync();
                            
                        ViewBag.History = history;
                        
                        return View(card);
                    }
                }
                else
                {
                    // Keep the existing image if no new one was uploaded
                    card.ImagePath = existingCard.ImagePath;
                }

                // Store QR color preferences in TempData for the session
                TempData["QrFgColor"] = card.QrFgColor;
                TempData["QrBgColor"] = card.QrBgColor;

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
        }

        // GET: Card/Delete/5 - Shows deletion confirmation
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Use QR colors from card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";
            
            // Store in TempData for subsequent requests
            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;
            
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

            // Use QR colors from card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";
            
            // Store in TempData for subsequent requests
            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            // Generate QR code for printable tag with custom colors
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            return View(card);
        }

        // GET: Card/PrintAll - Print all card tags
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();

            // Generate QR codes for all cards with their custom colors
            var qrCodes = new Dictionary<int, string>();
            foreach (var card in cards)
            {
                string qrFgColor = card.QrFgColor ?? "#000000";
                string qrBgColor = card.QrBgColor ?? "#FFFFFF";
                
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

            // Use QR colors from card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";
            
            // Store in TempData for subsequent requests
            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

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
            
            // Use QR colors from card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";
            
            // Store in TempData for subsequent requests
            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;
            
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
                LastUpdated = card.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Appearance = new {
                    Layout = card.Layout,
                    BackgroundColor = card.BackgroundColor,
                    TextColor = card.TextColor,
                    AccentColor = card.AccentColor,
                    QrForegroundColor = card.QrFgColor,
                    QrBackgroundColor = card.QrBgColor
                }
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

            // Use QR colors from card database values
            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

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

        public async Task<IActionResult> Reminders(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            var reminders = await _context.MaintenanceReminders
                .Where(r => r.CardId == id)
                .OrderBy(r => r.DueDate)
                .ToListAsync();
            
            ViewBag.Card = card;
            return View(reminders);
        }

        // GET: Card/GetCardReminders/5
        [HttpGet]
        public async Task<IActionResult> GetCardReminders(int id)
        {
            try
            {
                var reminders = await _context.MaintenanceReminders
                    .Where(r => r.CardId == id)
                    .OrderBy(r => r.DueDate)
                    .ToListAsync();
                
                return Json(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving reminders for card {id}");
                return StatusCode(500, new { error = "An error occurred while retrieving reminders." });
            }
        }

        // POST: Card/SaveReminder
        [HttpPost]
        public async Task<IActionResult> SaveReminder(MaintenanceReminder reminder)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (reminder.Id == 0)
                {
                    // New reminder
                    reminder.CreatedAt = DateTime.Now;
                    reminder.UpdatedAt = DateTime.Now;
                    reminder.CreatedBy = User.Identity.Name;
                    
                    _context.MaintenanceReminders.Add(reminder);
                }
                else
                {
                    // Update existing reminder
                    var existingReminder = await _context.MaintenanceReminders.FindAsync(reminder.Id);
                    
                    if (existingReminder == null)
                    {
                        return NotFound();
                    }
                    
                    existingReminder.Title = reminder.Title;
                    existingReminder.DueDate = reminder.DueDate;
                    existingReminder.Notes = reminder.Notes;
                    existingReminder.RepeatFrequency = reminder.RepeatFrequency;
                    existingReminder.UpdatedAt = DateTime.Now;
                    
                    _context.Update(existingReminder);
                }
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Reminder saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reminder");
                return StatusCode(500, new { error = "An error occurred while saving the reminder." });
            }
        }

        // POST: Card/DeleteReminder
        [HttpPost]
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
                
                return Json(new { success = true, message = "Reminder deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting reminder {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the reminder." });
            }
        }

        // GET: Card/Documents/5
        public async Task<IActionResult> Documents(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            var documents = await _context.CardDocuments
                .Where(d => d.CardId == id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
            
            ViewBag.Card = card;
            return View(documents);
        }

        // GET: Card/GetCardDocuments/5
        [HttpGet]
        public async Task<IActionResult> GetCardDocuments(int id)
        {
            try
            {
                var documents = await _context.CardDocuments
                    .Where(d => d.CardId == id)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
                
                return Json(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving documents for card {id}");
                return StatusCode(500, new { error = "An error occurred while retrieving documents." });
            }
        }

        // POST: Card/UploadDocument
        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromForm] CardDocument document)
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
                
                return Json(new { success = true, document = newDocument });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { error = "An error occurred while uploading the document." });
            }
        }

        // POST: Card/DeleteDocument
        [HttpPost]
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
                
                return Json(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document {id}");
                return StatusCode(500, new { error = "An error occurred while deleting the document." });
            }
        }

        // GET: Card/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.CardDocuments.FindAsync(id);
                
                if (document == null)
                {
                    return NotFound();
                }
                
                // For actual file download, you would use the document.FilePath to retrieve the file
                // This is a simplified implementation and may need to be customized based on your storage solution
                
                // Redirect to the file URL
                return Redirect(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading document {id}");
                return RedirectToAction("Details", new { id = id });
            }
        }        
        [HttpGet]
        public async Task<IActionResult> GetCardHistory(int id)
        {
            try
            {
                var history = await _context.CardHistories
                    .Where(h => h.CardId == id)
                    .OrderByDescending(h => h.ChangedAt)
                    .Take(10)
                    .ToListAsync();
                
                return Json(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving history for card {id}");
                return StatusCode(500, new { error = "An error occurred while retrieving history." });
            }
        }
    }
}