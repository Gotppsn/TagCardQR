// Path: Controllers/CardController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using CardTagManager.Data;
using CardTagManager.Models;
using CardTagManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
            if (ModelState.IsValid)
            {
                try
                {
                    // Set creator info
                    card.CreatedBy = User.Identity.Name;
                    
                    // Handle file upload
                    if (card.ImageFile != null)
                    {
                        // Upload to service
                        var uploadResult = await _fileUploadService.UploadFile(card.ImageFile);
                        if (uploadResult.IsSuccess)
                        {
                            card.ImagePath = uploadResult.FileUrl;
                        }
                        else
                        {
                            _logger.LogError($"Failed to upload image: {uploadResult.ErrorMessage}");
                            ModelState.AddModelError("ImageFile", $"Failed to upload image: {uploadResult.ErrorMessage}");
                            return View(card);
                        }
                    }

                    // Set timestamps
                    card.CreatedAt = DateTime.Now;
                    card.UpdatedAt = DateTime.Now;

                    // Add to database
                    _context.Cards.Add(card);
                    await _context.SaveChangesAsync();

                    // Set success message for UI
                    TempData["SuccessMessage"] = $"Product '{card.ProductName}' created successfully.";
                    
                    // Redirect to the detail page
                    return RedirectToAction(nameof(Detail), new { id = card.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating card");
                    ModelState.AddModelError("", "An error occurred while saving the product. Please try again.");
                    return View(card);
                }
            }

            // If validation fails, redisplay form
            return View(card);
        }

        // GET: Card/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

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
            
            return View(card);
        }

        // POST: Card/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Card card)
        {
            if (id != card.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image update if a new file is provided
                    if (card.ImageFile != null)
                    {
                        var uploadResult = await _fileUploadService.UploadFile(card.ImageFile);
                        if (uploadResult.IsSuccess)
                        {
                            // If there's an existing image, delete it (optional)
                            if (!string.IsNullOrEmpty(card.ImagePath))
                            {
                                await _fileUploadService.DeleteFile(card.ImagePath);
                            }
                            
                            card.ImagePath = uploadResult.FileUrl;
                        }
                        else
                        {
                            ModelState.AddModelError("ImageFile", $"Failed to upload image: {uploadResult.ErrorMessage}");
                            return View(card);
                        }
                    }

                    // Update timestamp
                    card.UpdatedAt = DateTime.Now;
                    
                    // Update in database
                    _context.Update(card);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Product '{card.ProductName}' updated successfully.";
                    return RedirectToAction(nameof(Detail), new { id = card.Id });
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
                    _logger.LogError(ex, "Error updating card");
                    ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
                    return View(card);
                }
            }
            return View(card);
        }

        // GET: Card/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }

        // GET: Card/PrintAll
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();
            return View(cards);
        }

        // Helper method to check if a card exists
        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }
    }
}