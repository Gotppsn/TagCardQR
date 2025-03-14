using Microsoft.AspNetCore.Mvc;
using ProductTagManager.Models;
using ProductTagManager.Services;
using System;
using System.Collections.Generic;

namespace ProductTagManager.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductRepository _productRepository;
        private readonly QrCodeService _qrCodeService;

        /// <summary>
        /// Constructor with dependency injection for product repository and QR code service
        /// </summary>
        public ProductController(ProductRepository productRepository, QrCodeService qrCodeService)
        {
            _productRepository = productRepository;
            _qrCodeService = qrCodeService;
        }

        // GET: Product - Main product listing page
        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        // GET: Product/Details/5 - Shows detailed product information
        public IActionResult Details(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            // Generate QR code for product details view
            string qrCodeData = GenerateProductQrData(product);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(product);
        }

        // GET: Product/Create - Shows product creation form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create - Handles form submission for new product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _productRepository.Add(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/Edit/5 - Shows product editing form
        public IActionResult Edit(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5 - Handles form submission for product updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _productRepository.Update(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Product/Delete/5 - Shows deletion confirmation
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5 - Handles product deletion
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Print/5 - Generates printable product tag
        public IActionResult Print(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            // Generate QR code for printable tag
            string qrCodeData = GenerateProductQrData(product);
            ViewBag.QrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);

            return View(product);
        }

        // GET: Product/PrintAll - Print all product tags
        public IActionResult PrintAll()
        {
            var products = _productRepository.GetAll();
            
            // Generate QR codes for all products
            var qrCodes = new Dictionary<int, string>();
            foreach (var product in products)
            {
                string qrCodeData = GenerateProductQrData(product);
                qrCodes[product.Id] = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
            }
            
            ViewBag.QrCodes = qrCodes;
            
            return View(products);
        }

        // GET: Product/QrCode/5 - Dedicated QR code display for scanning
        public IActionResult QrCode(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            // Generate QR code data
            string qrCodeData = GenerateProductQrData(product);
            string qrCodeImage = _qrCodeService.GenerateQrCodeAsBase64(qrCodeData);
            
            ViewBag.QrCodeImage = qrCodeImage;
            ViewBag.ProductName = product.ProductName;
            
            return View(product);
        }

        // Helper method to generate product data for QR code
        private string GenerateProductQrData(Product product)
        {
            string productData = $"PRODUCT:{product.ProductName}\n" +
                               $"CATEGORY:{product.Category}\n" +
                               $"MANUFACTURER:{product.Manufacturer}\n" +
                               $"MODEL:{product.ModelNumber}\n" +
                               $"SERIAL:{product.SerialNumber}\n" +
                               $"LOCATION:{product.Location}\n" +
                               $"MFGDATE:{product.ManufactureDate:yyyy-MM-dd}\n" +
                               $"PURCHDATE:{product.PurchaseDate:yyyy-MM-dd}\n" +
                               $"WARRANTY:{product.WarrantyExpiration:yyyy-MM-dd}\n";

            if (!string.IsNullOrEmpty(product.MaintenanceInfo))
            {
                productData += $"MAINTENANCE:{product.MaintenanceInfo}\n";
            }
            
            return productData;
        }

        // Generate downloadable product data file
        public IActionResult DownloadData(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            // Generate product data content
            string productDataContent = GenerateProductQrData(product);
            
            // Create a file name
            string fileName = $"{product.ProductName.Replace(" ", "_")}_Info.txt";
            
            // Return the data as a downloadable file
            return File(System.Text.Encoding.UTF8.GetBytes(productDataContent), "text/plain", fileName);
        }
    }
}