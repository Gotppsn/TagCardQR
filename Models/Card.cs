using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class Card
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Company { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [Phone]
        public string Phone { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        [Url]
        public string Website { get; set; }
        
        public string BackgroundColor { get; set; } = "#ffffff";
        
        public string TextColor { get; set; } = "#000000";
        
        public string AccentColor { get; set; } = "#0284c7";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}