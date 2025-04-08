// Path: Models/MaintenanceReminder.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
    public class MaintenanceReminder
    {
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        // Navigation property made nullable to prevent validation errors
        public Card? Card { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        
        public string Notes { get; set; }
        
        [StringLength(20)]
        public string RepeatFrequency { get; set; } = "never";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }
}