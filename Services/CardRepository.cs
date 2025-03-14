using System;
using System.Collections.Generic;
using System.Linq;
using CardTagManager.Models;

namespace CardTagManager.Services
{
    public class CardRepository
    {
        private static List<Card> _cards = new List<Card>
        {
            // Chemical Products
            new Card
            {
                Id = 1,
                Name = "RustShield Pro 5000",
                Category = "Rust Coating Chemical",
                Company = "ChemTech Industries",
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
            
            // Application Equipment
            new Card
            {
                Id = 2,
                Name = "Industrial Spray Booth",
                Category = "Application Equipment",
                Company = "SprayTech",
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
            
            // Lab Equipment with Expired Warranty
            new Card
            {
                Id = 3,
                Name = "Precision Lab Scale",
                Category = "Lab Equipment",
                Company = "LabTech Solutions",
                ModelNumber = "PLS-500",
                SerialNumber = "LTS2023450",
                Location = "Quality Control Lab",
                MaintenanceInfo = "Calibrate monthly using standard weights",
                ManufactureDate = new DateTime(2023, 5, 12),
                PurchaseDate = new DateTime(2023, 6, 1),
                WarrantyExpiration = new DateTime(2024, 6, 1),
                BackgroundColor = "#f0f9ff",
                TextColor = "#0c4a6e",
                AccentColor = "#0ea5e9"
            },
            
            // Safety Equipment
            new Card
            {
                Id = 4,
                Name = "Emergency Eyewash Station",
                Category = "Safety Equipment",
                Company = "SafetyFirst Inc.",
                ModelNumber = "EW-100",
                SerialNumber = "SF20245582",
                Location = "Chemical Processing Area",
                MaintenanceInfo = "Weekly testing required. Monthly full inspection.",
                ManufactureDate = new DateTime(2024, 9, 10),
                PurchaseDate = new DateTime(2024, 10, 15),
                WarrantyExpiration = new DateTime(2029, 10, 15),
                BackgroundColor = "#ffedd5",
                TextColor = "#7c2d12",
                AccentColor = "#ea580c"
            },
            
            // Coating Chemical
            new Card
            {
                Id = 5,
                Name = "CorrosionGuard Ultimate",
                Category = "Rust Coating Chemical",
                Company = "ProtectChem Corp",
                ModelNumber = "CG-ULT-20L",
                SerialNumber = "PC20250089",
                Location = "Chemical Storage Room B",
                MaintenanceInfo = "Keep sealed. Use within 6 months of opening.",
                ManufactureDate = new DateTime(2025, 2, 20),
                PurchaseDate = new DateTime(2025, 3, 5),
                WarrantyExpiration = new DateTime(2026, 3, 5),
                BackgroundColor = "#ecfccb",
                TextColor = "#365314",
                AccentColor = "#84cc16"
            },
            
            // Application Equipment with Warranty Expiring Soon
            new Card
            {
                Id = 6,
                Name = "Automated Coating Applicator",
                Category = "Application Equipment",
                Company = "AutoCoat Systems",
                ModelNumber = "ACA-3000",
                SerialNumber = "AC20241025",
                Location = "Production Floor, Zone 2",
                MaintenanceInfo = "Lubricate moving parts weekly. Full service every 6 months.",
                ManufactureDate = new DateTime(2024, 7, 15),
                PurchaseDate = new DateTime(2024, 8, 1),
                WarrantyExpiration = new DateTime(2025, 4, 1),
                BackgroundColor = "#1e293b",
                TextColor = "#f8fafc",
                AccentColor = "#38bdf8"
            },
            
            // Quality Control Equipment
            new Card
            {
                Id = 7,
                Name = "Coating Thickness Analyzer",
                Category = "Quality Control",
                Company = "PrecisionTech",
                ModelNumber = "CTA-X1",
                SerialNumber = "PT20244578",
                Location = "QC Testing Room",
                MaintenanceInfo = "Calibrate before each use. Replace sensor annually.",
                ManufactureDate = new DateTime(2024, 6, 10),
                PurchaseDate = new DateTime(2024, 7, 12),
                WarrantyExpiration = new DateTime(2026, 7, 12),
                BackgroundColor = "#eff6ff",
                TextColor = "#1e3a8a",
                AccentColor = "#3b82f6"
            },
            
            // Office Equipment
            new Card
            {
                Id = 8,
                Name = "Label Printer Industrial",
                Category = "Office Equipment",
                Company = "OfficeMax Pro",
                ModelNumber = "LPI-500",
                SerialNumber = "OM20252245",
                Location = "Shipping Department",
                MaintenanceInfo = "Clean print head weekly. Replace ribbon monthly.",
                ManufactureDate = new DateTime(2025, 1, 25),
                PurchaseDate = new DateTime(2025, 2, 15),
                WarrantyExpiration = new DateTime(2027, 2, 15),
                BackgroundColor = "#ffffff",
                TextColor = "#374151",
                AccentColor = "#ef4444"
            },
            
            // Safety Equipment with Long Warranty
            new Card
            {
                Id = 9,
                Name = "Chemical Storage Cabinet",
                Category = "Safety Equipment",
                Company = "ChemSafe Solutions",
                ModelNumber = "CSC-400L",
                SerialNumber = "CSS20245002",
                Location = "Chemical Storage Area",
                MaintenanceInfo = "Check ventilation monthly. Inspect seals quarterly.",
                ManufactureDate = new DateTime(2024, 10, 5),
                PurchaseDate = new DateTime(2024, 11, 10),
                WarrantyExpiration = new DateTime(2034, 11, 10),
                BackgroundColor = "#fef2f2",
                TextColor = "#991b1b",
                AccentColor = "#dc2626"
            },
            
            // Lab Equipment
            new Card
            {
                Id = 10,
                Name = "Viscosity Meter Digital",
                Category = "Lab Equipment",
                Company = "LabTech Solutions",
                ModelNumber = "VMD-200",
                SerialNumber = "LTS20244290",
                Location = "R&D Laboratory",
                MaintenanceInfo = "Calibrate monthly. Clean sensor after each use.",
                ManufactureDate = new DateTime(2024, 8, 18),
                PurchaseDate = new DateTime(2024, 9, 5),
                WarrantyExpiration = new DateTime(2026, 9, 5),
                BackgroundColor = "#f5f3ff",
                TextColor = "#5b21b6",
                AccentColor = "#8b5cf6"
            },
            
            // Recently Added Equipment
            new Card
            {
                Id = 11,
                Name = "Infrared Coating Dryer",
                Category = "Application Equipment",
                Company = "ThermalTech Inc.",
                ModelNumber = "ICD-500",
                SerialNumber = "TT20251001",
                Location = "Production Floor, Zone 3",
                MaintenanceInfo = "Check heating elements monthly. Clean reflectors weekly.",
                ManufactureDate = new DateTime(2025, 2, 1),
                PurchaseDate = new DateTime(2025, 3, 1),
                WarrantyExpiration = new DateTime(2028, 3, 1),
                BackgroundColor = "#fdf2f8",
                TextColor = "#831843",
                AccentColor = "#ec4899",
                CreatedAt = DateTime.Now.AddDays(-1),
                UpdatedAt = DateTime.Now.AddDays(-1)
            },
            
            // Specialized Chemical
            new Card
            {
                Id = 12,
                Name = "HardCoat Extreme Finish",
                Category = "Coating Chemical",
                Company = "AdvancedCoat Corp",
                ModelNumber = "HCE-50L",
                SerialNumber = "AC20250075",
                Location = "Special Materials Storage",
                MaintenanceInfo = "Temperature controlled storage required (18-22°C). Rotate stock quarterly.",
                ManufactureDate = new DateTime(2025, 2, 10),
                PurchaseDate = new DateTime(2025, 2, 28),
                WarrantyExpiration = new DateTime(2026, 8, 28),
                BackgroundColor = "#f3f4f6",
                TextColor = "#111827",
                AccentColor = "#10b981"
            }
        };

        public List<Card> GetAll()
        {
            return _cards;
        }

        public Card GetById(int id)
        {
            return _cards.FirstOrDefault(p => p.Id == id);
        }

        public Card Add(Card card)
        {
            int newId = _cards.Count > 0 ? _cards.Max(p => p.Id) + 1 : 1;
            card.Id = newId;
            card.CreatedAt = DateTime.Now;
            card.UpdatedAt = DateTime.Now;
            
            _cards.Add(card);
            return card;
        }

        public Card Update(Card card)
        {
            var existingCard = _cards.FirstOrDefault(p => p.Id == card.Id);
            if (existingCard == null)
                return null;
                
            existingCard.Name = card.Name;
            existingCard.Category = card.Category;
            existingCard.Company = card.Company;
            existingCard.ModelNumber = card.ModelNumber;
            existingCard.SerialNumber = card.SerialNumber;
            existingCard.Location = card.Location;
            existingCard.MaintenanceInfo = card.MaintenanceInfo;
            existingCard.ManufactureDate = card.ManufactureDate;
            existingCard.PurchaseDate = card.PurchaseDate;
            existingCard.WarrantyExpiration = card.WarrantyExpiration;
            existingCard.BackgroundColor = card.BackgroundColor;
            existingCard.TextColor = card.TextColor;
            existingCard.AccentColor = card.AccentColor;
            existingCard.UpdatedAt = DateTime.Now;
            
            return existingCard;
        }

        public bool Delete(int id)
        {
            var card = _cards.FirstOrDefault(p => p.Id == id);
            if (card == null)
                return false;
                
            _cards.Remove(card);
            return true;
        }
    }
}