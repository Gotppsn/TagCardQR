using CardTagManager.Models;
using System;
using System.Linq;

namespace CardTagManager.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Look for any cards
            if (context.Cards.Any())
            {
                return;   // DB has been seeded
            }

            var cards = new Card[]
            {
                new Card
                {
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
                    AccentColor = "#0284c7",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                
                new Card
                {
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
                    AccentColor = "#6610f2",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                
                // Add more seed data as needed
            };

            context.Cards.AddRange(cards);
            context.SaveChanges();
        }
    }
}