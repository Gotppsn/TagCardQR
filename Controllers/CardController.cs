using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using CardTagManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CardTagManager.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QrCodeService _qrCodeService;
        private readonly FileUploadService _fileUploadService;

        public CardController(ApplicationDbContext context, QrCodeService qrCodeService, FileUploadService fileUploadService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _fileUploadService = fileUploadService;
        }

        // GET: Card - Main card listing page
        public async Task<IActionResult> Index()
        {
            var cards = await _context.Cards.ToListAsync();
            return View(cards);
        }

        // GET: Card/Details/5 - Shows detailed card information
        public async Task<IActionResult> Details(int id)
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

        // GET: Card/Create - Shows card creation form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Card/Create - Handles form submission for new card
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload if a file was provided
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    try
                    {
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

                card.CreatedAt = DateTime.Now;
                card.UpdatedAt = DateTime.Now;
                
                _context.Add(card);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
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
                    // Get existing card to check if we need to delete old image
                    var existingCard = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    
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
            var card = await _context.Cards.FindAsync(id);
            if (card != null)
            {
                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
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
            ViewBag.CardName = card.Name;

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
                // Other demo results...
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

        // Helper method to generate card data for QR code
        private string GenerateCardQrData(Card card)
        {
            string cardData = $"CARD:{card.Name}\n" +
                               $"CATEGORY:{card.Category}\n" +
                               $"COMPANY:{card.Company}\n" +
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
            string fileName = $"{card.Name.Replace(" ", "_")}_Info.txt";

            // Return the data as a downloadable file
            return File(System.Text.Encoding.UTF8.GetBytes(cardDataContent), "text/plain", fileName);
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
        
        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }
    }
}