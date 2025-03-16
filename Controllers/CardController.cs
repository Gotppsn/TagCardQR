using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using CardTagManager.Services;
using System;
using System.Collections.Generic;

namespace CardTagManager.Controllers
{
    public class CardController : Controller
    {
        private readonly CardRepository _cardRepository;
        private readonly QrCodeService _qrCodeService;

        public CardController(CardRepository cardRepository, QrCodeService qrCodeService)
        {
            _cardRepository = cardRepository;
            _qrCodeService = qrCodeService;
        }

        // GET: Card - Main card listing page
        public IActionResult Index()
        {
            var cards = _cardRepository.GetAll();
            return View(cards);
        }

        // GET: Card/Details/5 - Shows detailed card information
        public IActionResult Details(int id)
        {
            var card = _cardRepository.GetById(id);
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
        public IActionResult Create(Card card)
        {
            if (ModelState.IsValid)
            {
                _cardRepository.Add(card);
                return RedirectToAction(nameof(Index));
            }
            return View(card);
        }

        // GET: Card/Edit/5 - Shows card editing form
        public IActionResult Edit(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }
            return View(card);
        }

        // POST: Card/Edit/5 - Handles form submission for card updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Card card)
        {
            if (id != card.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _cardRepository.Update(card);
                return RedirectToAction(nameof(Index));
            }
            return View(card);
        }

        // GET: Card/Delete/5 - Shows deletion confirmation
        public IActionResult Delete(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }

        // POST: Card/Delete/5 - Handles card deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _cardRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Card/Print/5 - Generates printable card tag
        public IActionResult Print(int id)
        {
            var card = _cardRepository.GetById(id);
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
        public IActionResult PrintAll()
        {
            var cards = _cardRepository.GetAll();

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
        public IActionResult QrCode(int id)
        {
            var card = _cardRepository.GetById(id);
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

// Add this to CardController.cs
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
            Id = 2,
            CardId = 3,
            CardName = "Precision Lab Scale",
            ScanTime = DateTime.Now.AddDays(-1),
            DeviceInfo = "Samsung Galaxy S22, Android 13",
            Location = "Quality Control Lab",
            ScanResult = "Success"
        },
        new ScanResultViewModel
        {
            Id = 3,
            CardId = 4,
            CardName = "Emergency Eyewash Station",
            ScanTime = DateTime.Now.AddHours(-5),
            DeviceInfo = "iPad Air, iOS 16.4",
            Location = "Chemical Processing Area",
            ScanResult = "Success"
        },
        new ScanResultViewModel
        {
            Id = 4,
            CardId = 2,
            CardName = "Industrial Spray Booth",
            ScanTime = DateTime.Today,
            DeviceInfo = "Google Pixel 7, Android 13",
            Location = "Building 2, Bay 4",
            ScanResult = "Failed"
        },
        new ScanResultViewModel
        {
            Id = 5,
            CardId = 5,
            CardName = "CorrosionGuard Ultimate",
            ScanTime = DateTime.Now.AddMinutes(-30),
            DeviceInfo = "iPhone 13, iOS 16.5",
            Location = "Chemical Storage Room B",
            ScanResult = "Success"
        },
        new ScanResultViewModel
        {
            Id = 6,
            CardId = 7,
            CardName = "Coating Thickness Analyzer",
            ScanTime = DateTime.Today.AddHours(-1),
            DeviceInfo = "Samsung Galaxy Tab, Android 12",
            Location = "QC Testing Room",
            ScanResult = "Partial"
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
        public IActionResult DownloadData(int id)
        {
            var card = _cardRepository.GetById(id);
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
        
        public IActionResult ScanShow(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }
            
            // Generate QR code for printable tag
            string qrCodeData = GenerateCardQrData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(card);
        }
    }
}