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
            // Sample card
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
            
            // Sample equipment
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