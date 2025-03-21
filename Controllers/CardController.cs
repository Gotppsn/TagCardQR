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

                    // Ensure CustomFieldsData is valid JSON
                    if (string.IsNullOrEmpty(card.CustomFieldsData))
                    {
                        card.CustomFieldsData = "{}";
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
                        return RedirectToAction(nameof(Detail), new { id = card.Id });
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