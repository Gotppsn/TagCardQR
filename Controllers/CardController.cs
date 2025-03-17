using System;
using System.Diagnostics;
using System.Linq;
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

                // Generate QR code for card details view
                string qrCodeData = GenerateCardQrData(card);
                ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

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
            if (ModelState.IsValid)
            {
                // Set user information from claims
                if (User.Identity.IsAuthenticated)
                {
                    card.Username = User.FindFirstValue("Username") ?? User.Identity.Name;
                    card.Department = User.FindFirstValue("Department") ?? "";
                    card.Email = User.FindFirstValue("Email") ?? "";
                    card.UserFullName = User.FindFirstValue("FullName") ?? "";
                    card.PlantName = User.FindFirstValue("PlantName") ?? "";
                }
            try
            {
                _logger.LogInformation($"Creating card: {card.ProductName}, Category: {card.Category}");
                
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
                    
                    // Debug log before adding to context
                    _logger.LogInformation($"About to add card to DB: {card.ProductName}, Manufacturer: {card.Manufacturer}");
                    
                    try
                    {
                        // Add the card to the context
                        _context.Cards.Add(card);
                        
                        // Save the changes to the database
                        int result = await _context.SaveChangesAsync();
                        
                        // Check if SaveChanges succeeded
                        if (result > 0)
                        {
                            _logger.LogInformation($"Card saved successfully with ID: {card.Id}");
                            TempData["SuccessMessage"] = $"Product '{card.ProductName}' created successfully.";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            _logger.LogWarning("SaveChanges returned 0 affected rows");
                            ModelState.AddModelError(string.Empty, "No changes were saved to the database.");
                            return View(card);
                        }
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Database error when saving card");
                        ModelState.AddModelError(string.Empty, "Database error: " + (dbEx.InnerException?.Message ?? dbEx.Message));
                        return View(card);
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
            
            // Generate QR code for preview
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
            
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing card
                    var existingCard = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    
                    // Keep the user information from the database
                    card.Username = existingCard.Username;
                    card.Department = existingCard.Department;
                    card.Email = existingCard.Email;
                    card.UserFullName = existingCard.UserFullName;
                    card.PlantName = existingCard.PlantName;
                    
                    // Handle image upload if a file was provided
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        try
                        {
                            // Delete old image if it exists
                            if (!string.IsNullOrEmpty(existingCard?.ImagePath))
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
                        card.ImagePath = existingCard?.ImagePath;
                    }

                    card.UpdatedAt = DateTime.Now;
                    _context.Update(card);
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
            
            // Generate QR code for preview
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

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

            // Generate QR code for printable tag
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(card);
        }

        // GET: Card/PrintAll - Print all card tags
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();

            // Generate QR codes for all cards
            var qrCodes = new Dictionary<int, string>();
            foreach (var card in cards)
            {
                string qrCodeData = GenerateCardQrData(card);
                qrCodes[card.Id] = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
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

            // Generate QR code data
            string qrCodeData = GenerateCardQrData(card);
            string qrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            ViewBag.QrCodeImage = qrCodeImage;
            ViewBag.CardName = card.ProductName;

            return View(card);
        }

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
        
        public async Task<IActionResult> ScanShow(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Generate QR code for printable tag
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(card);
        }
        
        // Generate downloadable card data file
        public async Task<IActionResult> DownloadData(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            // Generate card data content
            string cardDataContent = GenerateCardQrData(card);

            // Create a file name
            string fileName = $"{card.ProductName.Replace(" ", "_")}_Info.txt";

            // Return the data as a downloadable file
            return File(System.Text.Encoding.UTF8.GetBytes(cardDataContent), "text/plain", fileName);
        }

        // Helper method to generate card data for QR code
        private string GenerateCardQrData(Card card)
        {
            string cardData = $"CARD:{card.ProductName}\n" +
                               $"CATEGORY:{card.Category}\n" +
                               $"COMPANY:{card.Manufacturer}\n" +
                               $"MODEL:{card.ModelNumber}\n" +
                               $"SERIAL:{card.SerialNumber}\n" +
                               $"LOCATION:{card.Location}\n" +
                               $"MFGDATE:{card.ManufactureDate:yyyy-MM-dd}\n" +
                               $"PURCHDATE:{card.PurchaseDate:yyyy-MM-dd}\n" +
                               $"WARRANTY:{card.WarrantyExpiration:yyyy-MM-dd}\n";

            if (!string.IsNullOrEmpty(card.MaintenanceInfo))
            {
                cardData += $"MAINTENANCE:{card.MaintenanceInfo}\n";
            }

            return cardData;
        }

        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }
    }
}