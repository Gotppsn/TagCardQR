// Path: Models/Card.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace CardTagManager.Models
{
    public class Card
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100)]
        public string ProductName { get; set; }
        
        [StringLength(100)]
        public string Category { get; set; }
        
        public string Location { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime ManufactureDate { get; set; } = DateTime.Now;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime WarrantyExpiration { get; set; } = DateTime.Now.AddYears(1);
        
        public string MaintenanceInfo { get; set; }
        
        [StringLength(20)]
        public string BackgroundColor { get; set; } = "#ffffff";
        
        [StringLength(20)]
        public string TextColor { get; set; } = "#000000";
        
        [StringLength(20)]
        public string AccentColor { get; set; } = "#0284c7";
        
        [StringLength(20)]
        public string QrFgColor { get; set; } = "#000000";
        
        [StringLength(20)]
        public string QrBgColor { get; set; } = "#ffffff";
        
        public string ImagePath { get; set; } = string.Empty; // Added default value
        
        [NotMapped]
        public IFormFile ImageFile { get; set; }
        
        // Store custom fields from templates as JSON
        [Column(TypeName = "nvarchar(max)")]
        public string CustomFieldsData { get; set; } = "{}";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Creator information
        public string CreatedBy { get; set; } = string.Empty; // Added default value
    }
}