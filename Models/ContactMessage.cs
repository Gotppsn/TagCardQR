// Path: Models/ContactMessage.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public Card Product { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        public DateTime? ReadAt { get; set; }
        
        public string ResponseStatus { get; set; } = "Pending";
    }
}