// Path: Controllers/CardController.cs
using System;
using System.Collections.Generic;
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

        // GET: Card
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

        // GET: Card/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var card = await _context.Cards.FindAsync(id);
                if (card == null)
                {
                    return NotFound();
                }

                string qrFgColor = card.QrFgColor ?? "#000000";
                string qrBgColor = card.QrBgColor ?? "#FFFFFF";

                TempData["QrFgColor"] = qrFgColor;
                TempData["QrBgColor"] = qrBgColor;

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

        // GET: Card/Create
        public IActionResult Create()
        {
            var card = new Card();

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

        // POST: Card/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Card card)
        {
            try
            {
                _logger.LogInformation($"Creating card: {card.ProductName}, Category: {card.Category}");

                if (User.Identity.IsAuthenticated)
                {
                    card.Username = User.FindFirstValue("Username") ?? User.Identity.Name;
                    card.Department = User.FindFirstValue("Department") ?? "";
                    card.Email = User.FindFirstValue("Email") ?? "";
                    card.UserFullName = User.FindFirstValue("FullName") ?? "";
                    card.PlantName = User.FindFirstValue("PlantName") ?? "";
                }

                if (string.IsNullOrEmpty(card.BackgroundColor))
                    card.BackgroundColor = "#ffffff";

                if (string.IsNullOrEmpty(card.TextColor))
                    card.TextColor = "#000000";

                if (string.IsNullOrEmpty(card.AccentColor))
                    card.AccentColor = "#0284c7";

                if (string.IsNullOrEmpty(card.QrFgColor))
                    card.QrFgColor = "#000000";

                if (string.IsNullOrEmpty(card.QrBgColor))
                    card.QrBgColor = "#FFFFFF";

                ProcessCustomFields(card);

                if (ModelState.ContainsKey("ImageFile"))
                    ModelState.Remove("ImageFile");

                if (ModelState.ContainsKey("Email"))
                    ModelState.Remove("Email");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    _logger.LogWarning($"Model validation failed: {errors}");
                    return View(card);
                }

                if (card.ImageFile != null && card.ImageFile.Length > 0)
                {
                    try
                    {
                        var fileResponse = await _fileUploadService.UploadFile(card.ImageFile);
                        if (fileResponse.IsSuccess)
                        {
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

                TempData["QrFgColor"] = card.QrFgColor;
                TempData["QrBgColor"] = card.QrBgColor;

                _context.Cards.Add(card);
                await _context.SaveChangesAsync();

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

                TempData["SuccessMessage"] = $"Product '{card.ProductName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create action");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                return View(card);
            }
        }

        // GET: Card/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            var history = await _context.CardHistories
                .Where(ch => ch.CardId == id)
                .OrderByDescending(ch => ch.ChangedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.History = history;

            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

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

            ProcessCustomFields(card);

            if (ModelState.ContainsKey("ImageFile"))
                ModelState.Remove("ImageFile");

            if (ModelState.ContainsKey("Email"))
                ModelState.Remove("Email");

            if (!ModelState.IsValid)
            {
                string qrCodeData = GenerateCardQrData(card);
                ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(
                    qrCodeData,
                    card.QrFgColor ?? "#000000",
                    card.QrBgColor ?? "#FFFFFF"
                );

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
                var existingCard = await _context.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                if (existingCard == null)
                {
                    return NotFound();
                }

                card.Username = existingCard.Username;
                card.Department = existingCard.Department;
                card.Email = existingCard.Email;
                card.UserFullName = existingCard.UserFullName;
                card.PlantName = existingCard.PlantName;
                card.CreatedAt = existingCard.CreatedAt;

                if (string.IsNullOrEmpty(card.BackgroundColor))
                    card.BackgroundColor = existingCard.BackgroundColor ?? "#ffffff";

                if (string.IsNullOrEmpty(card.TextColor))
                    card.TextColor = existingCard.TextColor ?? "#000000";

                if (string.IsNullOrEmpty(card.AccentColor))
                    card.AccentColor = existingCard.AccentColor ?? "#0284c7";

                if (string.IsNullOrEmpty(card.QrFgColor))
                    card.QrFgColor = existingCard.QrFgColor ?? "#000000";

                if (string.IsNullOrEmpty(card.QrBgColor))
                    card.QrBgColor = existingCard.QrBgColor ?? "#FFFFFF";

                if (card.ImageFile != null && card.ImageFile.Length > 0)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(existingCard.ImagePath))
                        {
                            await _fileUploadService.DeleteFile(existingCard.ImagePath);
                        }

                        var fileResponse = await _fileUploadService.UploadFile(card.ImageFile);
                        if (fileResponse.IsSuccess)
                        {
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
                    card.ImagePath = existingCard.ImagePath;
                }

                TempData["QrFgColor"] = card.QrFgColor;
                TempData["QrBgColor"] = card.QrBgColor;

                var changedProperties = new List<CardHistory>();

                var propertiesToTrack = typeof(Card).GetProperties()
                    .Where(p =>
                        p.Name != "Id" &&
                        p.Name != "CreatedAt" &&
                        p.Name != "UpdatedAt" &&
                        p.Name != "Username" &&
                        p.Name != "Department" &&
                        p.Name != "Email" &&
                        p.Name != "UserFullName" &&
                        p.Name != "PlantName" &&
                        p.Name != "ImageFile")
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

        // GET: Card/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            return View(card);
        }

        // POST: Card/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var card = await _context.Cards.FindAsync(id);
                if (card != null)
                {
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

                    var histories = await _context.CardHistories.Where(h => h.CardId == id).ToListAsync();
                    if (histories.Any())
                    {
                        _context.CardHistories.RemoveRange(histories);
                    }

                    var reminders = await _context.MaintenanceReminders.Where(r => r.CardId == id).ToListAsync();
                    if (reminders.Any())
                    {
                        _context.MaintenanceReminders.RemoveRange(reminders);
                    }

                    var documents = await _context.CardDocuments.Where(d => d.CardId == id).ToListAsync();
                    if (documents.Any())
                    {
                        _context.CardDocuments.RemoveRange(documents);
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

        // GET: Card/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            return View(card);
        }

        // GET: Card/PrintAll
        public async Task<IActionResult> PrintAll()
        {
            var cards = await _context.Cards.ToListAsync();

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

        // GET: Card/QrCode/5
        public async Task<IActionResult> QrCode(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            string qrCodeData = GenerateCardQrData(card);
            string qrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            ViewBag.QrCodeImage = qrCodeImage;
            ViewBag.CardName = card.ProductName;

            return View(card);
        }

        // GET: Card/ScanShow/5
        public async Task<IActionResult> ScanShow(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            string qrFgColor = card.QrFgColor ?? "#000000";
            string qrBgColor = card.QrBgColor ?? "#FFFFFF";

            TempData["QrFgColor"] = qrFgColor;
            TempData["QrBgColor"] = qrBgColor;

            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData, qrFgColor, qrBgColor);

            _logger.LogInformation($"Card {id} ({card.ProductName}) was scanned at {DateTime.Now}");

            return View(card);
        }

        // Helper method to process custom fields from form
        private void ProcessCustomFields(Card card)
        {
            Dictionary<string, string> customFields = new Dictionary<string, string>();
            
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("custom-"))
                {
                    string fieldName = key.Substring("custom-".Length);
                    string fieldValue = Request.Form[key];
                    customFields.Add(fieldName, fieldValue);
                }
            }

            if (customFields.Count > 0)
            {
                card.CustomFieldsData = System.Text.Json.JsonSerializer.Serialize(customFields);
            }
        }

        // Helper method to generate card data for QR code
        private string GenerateCardQrData(Card card)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string scanUrl = $"{baseUrl}/Card/ScanShow/{card.Id}";
            return scanUrl;
        }

        private bool CardExists(int id)
        {
            return _context.Cards.Any(e => e.Id == id);
        }

        // GET: Card/History/5
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

        // GET: Card/Reminders/5
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

        // POST: Card/UploadDocument
        [HttpPost]
        public async Task<IActionResult> UploadDocument()
        {
            try
            {
                var cardId = int.Parse(Request.Form["CardId"]);
                var title = Request.Form["Title"].ToString();
                var documentType = Request.Form["DocumentType"].ToString();
                var description = Request.Form["Description"].ToString();
                var documentFile = Request.Form.Files["DocumentFile"];

                if (documentFile == null || documentFile.Length == 0)
                {
                    return Json(new { success = false, error = "No file was uploaded." });
                }

                if (string.IsNullOrEmpty(title) || cardId <= 0)
                {
                    return Json(new { success = false, error = "Missing required fields." });
                }

                var fileResponse = await _fileUploadService.UploadFile(documentFile, "CardDocuments");

                if (!fileResponse.IsSuccess)
                {
                    return Json(new { success = false, error = fileResponse.ErrorMessage });
                }

                var newDocument = new CardDocument
                {
                    CardId = cardId,
                    Title = title,
                    DocumentType = documentType,
                    Description = description,
                    FilePath = fileResponse.FileUrl,
                    FileName = documentFile.FileName,
                    FileSize = documentFile.Length,
                    FileType = documentFile.ContentType,
                    UploadedAt = DateTime.Now,
                    UploadedBy = User.Identity?.Name ?? "Anonymous"
                };

                _context.CardDocuments.Add(newDocument);
                await _context.SaveChangesAsync();

                return Json(new { success = true, document = newDocument });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return Json(new { success = false, error = $"An error occurred: {ex.Message}" });
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

        // POST: Card/SaveReminder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReminder([FromBody] MaintenanceReminder reminder)
        {
            ModelState.Remove("Card");
            
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() });
            }

            try
            {
                if (reminder.Id == 0)
                {
                    reminder.CreatedAt = DateTime.Now;
                    reminder.UpdatedAt = DateTime.Now;
                    reminder.CreatedBy = User.Identity?.Name ?? "System";
                    
                    _context.MaintenanceReminders.Add(reminder);
                }
                else
                {
                    var existingReminder = await _context.MaintenanceReminders.FindAsync(reminder.Id);
                    if (existingReminder == null) return NotFound(new { success = false, error = "Reminder not found" });

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
                return StatusCode(500, new { success = false, error = $"Database error: {ex.Message}" });
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

                return Redirect(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading document {id}");
                return RedirectToAction("Details", new { id = id });
            }
        }
    }
}