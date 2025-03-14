using System;
using System.Collections.Generic;
using System.Linq;
using ProductTagManager.Models;

namespace ProductTagManager.Services
{
    public class ProductRepository
    {
        private static List<Product> _products = new List<Product>
        {
            // Sample chemical product
            new Product
            {
                Id = 1,
                ProductName = "RustShield Pro 5000",
                Category = "Rust Coating Chemical",
                Manufacturer = "ChemTech Industries",
                ModelNumber = "RS-5000-X",
                SerialNumber = "CT20250001",
                Location = "Chemical Storage Room A",
                MaintenanceInfo = "Store at 10-15°C, away from direct sunlight",
                ManufactureDate = new DateTime(2025, 1, 15),
                PurchaseDate = new DateTime(2025, 2, 1),
                WarrantyExpiration = new DateTime(2027, 2, 1),
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                AccentColor = "#0284c7"
            },
            
            // Sample equipment
            new Product
            {
                Id = 2,
                ProductName = "Industrial Spray Booth",
                Category = "Application Equipment",
                Manufacturer = "SprayTech",
                ModelNumber = "SB-2000",
                SerialNumber = "ST2024789",
                Location = "Building 2, Bay 4",
                MaintenanceInfo = "Filter replacement every 3 months",
                ManufactureDate = new DateTime(2024, 11, 10),
                PurchaseDate = new DateTime(2024, 12, 5),
                WarrantyExpiration = new DateTime(2026, 12, 5),
                BackgroundColor = "#f8f9fa",
                TextColor = "#212529",
                AccentColor = "#6610f2"
            },
            
            // Sample industrial printer
            new Product
            {
                Id = 3,
                ProductName = "Industrial Label Printer",
                Category = "Office Equipment",
                Manufacturer = "TechPrint Solutions",
                ModelNumber = "TP-4500",
                SerialNumber = "TPS2025003",
                Location = "Office 203",
                MaintenanceInfo = "Replace toner cartridge when prompted",
                ManufactureDate = new DateTime(2024, 10, 5),
                PurchaseDate = new DateTime(2024, 10, 20),
                WarrantyExpiration = new DateTime(2026, 10, 20),
                BackgroundColor = "#f0fdf4",
                TextColor = "#166534",
                AccentColor = "#16a34a"
            },
            
            // Sample testing equipment
            new Product
            {
                Id = 4,
                ProductName = "Viscosity Tester",
                Category = "Lab Equipment",
                Manufacturer = "LabTech Instruments",
                ModelNumber = "VT-200",
                SerialNumber = "LT2024054",
                Location = "Quality Control Lab",
                MaintenanceInfo = "Calibrate monthly using standard solution",
                ManufactureDate = new DateTime(2024, 9, 15),
                PurchaseDate = new DateTime(2024, 9, 30),
                WarrantyExpiration = new DateTime(2026, 9, 30),
                BackgroundColor = "#eff6ff",
                TextColor = "#1e40af",
                AccentColor = "#3b82f6"
            }
        };

        public List<Product> GetAll()
        {
            return _products;
        }

        public Product GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public Product Add(Product product)
        {
            int newId = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
            product.Id = newId;
            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;
            
            _products.Add(product);
            return product;
        }

        public Product Update(Product product)
        {
            var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
                return null;
                
            existingProduct.ProductName = product.ProductName;
            existingProduct.Category = product.Category;
            existingProduct.Manufacturer = product.Manufacturer;
            existingProduct.ModelNumber = product.ModelNumber;
            existingProduct.SerialNumber = product.SerialNumber;
            existingProduct.Location = product.Location;
            existingProduct.MaintenanceInfo = product.MaintenanceInfo;
            existingProduct.ManufactureDate = product.ManufactureDate;
            existingProduct.PurchaseDate = product.PurchaseDate;
            existingProduct.WarrantyExpiration = product.WarrantyExpiration;
            existingProduct.BackgroundColor = product.BackgroundColor;
            existingProduct.TextColor = product.TextColor;
            existingProduct.AccentColor = product.AccentColor;
            existingProduct.UpdatedAt = DateTime.Now;
            
            return existingProduct;
        }

        public bool Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return false;
                
            _products.Remove(product);
            return true;
        }
    }
}