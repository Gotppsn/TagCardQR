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

        // GET: Card
        public IActionResult Index()
        {
            var cards = _cardRepository.GetAll();
            return View(cards);
        }

        // GET: Card/Details/5
        public IActionResult Details(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }

            // Generate QR code
            string qrCodeData = GenerateVCardData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(card);
        }

        // GET: Card/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Card/Create
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

        // GET: Card/Edit/5
        public IActionResult Edit(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }
            return View(card);
        }

        // POST: Card/Edit/5
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

        // GET: Card/Delete/5
        public IActionResult Delete(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }

        // POST: Card/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _cardRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Card/Print/5
        public IActionResult Print(int id)
        {
            var card = _cardRepository.GetById(id);
            if (card == null)
            {
                return NotFound();
            }

            // Generate QR code
            string qrCodeData = GenerateVCardData(card);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(card);
        }

        // GET: Card/PrintAll
        public IActionResult PrintAll()
        {
            var cards = _cardRepository.GetAll();
            
            // Generate QR codes for all cards
            var qrCodes = new Dictionary<int, string>();
            foreach (var card in cards)
            {
                string qrCodeData = GenerateVCardData(card);
                qrCodes[card.Id] = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
            }
            
            ViewBag.QrCodes = qrCodes;
            
            return View(cards);
        }

        // Helper method to generate vCard data for QR code
        private string GenerateVCardData(Card card)
        {
            string vcard = "BEGIN:VCARD\n" +
                          "VERSION:3.0\n" +
                          $"FN:{card.Name}\n" +
                          $"ORG:{card.Company}\n" +
                          $"TITLE:{card.Title}\n" +
                          $"TEL:{card.Phone}\n" +
                          $"EMAIL:{card.Email}\n";

            if (!string.IsNullOrEmpty(card.Address))
            {
                vcard += $"ADR:;;{card.Address};;;\n";
            }

            if (!string.IsNullOrEmpty(card.Website))
            {
                vcard += $"URL:{card.Website}\n";
            }

            vcard += "END:VCARD";
            return vcard;
        }
        public IActionResult Vcf(int id)
{
    var card = _cardRepository.GetById(id);
    if (card == null)
    {
        return NotFound();
    }

    // Generate vCard content
    string vCardContent = GenerateVCardData(card);
    
    // Create a file name
    string fileName = $"{card.Name.Replace(" ", "_")}_Contact.vcf";
    
    // Return the vCard as a downloadable file
    return File(System.Text.Encoding.UTF8.GetBytes(vCardContent), "text/vcard", fileName);
}

// GET: Card/QrCode/5
public IActionResult QrCode(int id)
{
    var card = _cardRepository.GetById(id);
    if (card == null)
    {
        return NotFound();
    }

    // Generate QR code data
    string qrCodeData = GenerateVCardData(card);
    string qrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
    
    // Strip the data URL prefix for the src attribute
    ViewBag.QrCodeImage = qrCodeImage;
    ViewBag.CardName = card.Name;
    
    return View(card);
}

// GET: Card/Tag/5?tag=value
public IActionResult Tag(int id, string tag)
{
    var card = _cardRepository.GetById(id);
    if (card == null)
    {
        return NotFound();
    }
    
    // Here you would implement tag functionality
    // This is a placeholder for the tag feature
    
    return RedirectToAction(nameof(Details), new { id });
}
    }
}