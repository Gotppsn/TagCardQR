using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class Card
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;
        
        [Url]
        public string Website { get; set; } = string.Empty;
        
        public string BackgroundColor { get; set; } = "#ffffff";
        
        public string TextColor { get; set; } = "#000000";
        
        public string AccentColor { get; set; } = "#0284c7";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}