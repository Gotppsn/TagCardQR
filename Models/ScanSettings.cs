// Path: Models/ScanSettings.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
    public class ScanSettings
    {
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        public Card Card { get; set; }
        
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string FieldsJson { get; set; } = "[]";
        
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string UiElementsJson { get; set; } = "[]";
        
        public bool PrivateMode { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }
}