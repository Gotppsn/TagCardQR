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
            new Card
            {
                Id = 1,
                Name = "John Doe",
                Title = "Senior Developer",
                Company = "Tech Solutions Inc.",
                Email = "john.doe@techsolutions.com",
                Phone = "+1 (555) 123-4567",
                Address = "123 Tech Boulevard, San Francisco, CA 94107",
                Website = "https://techsolutions.com",
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                AccentColor = "#0284c7"
            },
            new Card
            {
                Id = 2,
                Name = "Jane Smith",
                Title = "UX Designer",
                Company = "Creative Minds",
                Email = "jane.smith@creativeminds.com",
                Phone = "+1 (555) 987-6543",
                Address = "456 Design Avenue, New York, NY 10001",
                Website = "https://creativeminds.com",
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
            return _cards.FirstOrDefault(c => c.Id == id);
        }

        public Card Add(Card card)
        {
            int newId = _cards.Count > 0 ? _cards.Max(c => c.Id) + 1 : 1;
            card.Id = newId;
            card.CreatedAt = DateTime.Now;
            card.UpdatedAt = DateTime.Now;
            
            _cards.Add(card);
            return card;
        }

        public Card Update(Card card)
        {
            var existingCard = _cards.FirstOrDefault(c => c.Id == card.Id);
            if (existingCard == null)
                return null;
                
            existingCard.Name = card.Name;
            existingCard.Title = card.Title;
            existingCard.Company = card.Company;
            existingCard.Email = card.Email;
            existingCard.Phone = card.Phone;
            existingCard.Address = card.Address;
            existingCard.Website = card.Website;
            existingCard.BackgroundColor = card.BackgroundColor;
            existingCard.TextColor = card.TextColor;
            existingCard.AccentColor = card.AccentColor;
            existingCard.UpdatedAt = DateTime.Now;
            
            return existingCard;
        }

        public bool Delete(int id)
        {
            var card = _cards.FirstOrDefault(c => c.Id == id);
            if (card == null)
                return false;
                
            _cards.Remove(card);
            return true;
        }
    }
}