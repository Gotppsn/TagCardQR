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
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;
        
        // Original property as fallback for compatibility
        public string Name { 
            get => ProductName; 
            set => ProductName = value; 
        }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;
        
        // Original property as fallback for compatibility
        public string Company { 
            get => Manufacturer; 
            set => Manufacturer = value; 
        }
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Display(Name = "Model Number")]
        public string ModelNumber { get; set; } = string.Empty;
        
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;
        
        public string? ImagePath { get; set; } = null;
        
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;
        
        [Display(Name = "Maintenance Information")]
        public string MaintenanceInfo { get; set; } = string.Empty;
        
        // User information fields
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
        
        [EmailAddress]
        public string? Email { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string UserFullName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string PlantName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateTime ManufactureDate { get; set; } = DateTime.Now;
        
        [Required]
        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        [Required]
        [Display(Name = "Warranty Expiration")]
        [DataType(DataType.Date)]
        public DateTime WarrantyExpiration { get; set; } = DateTime.Now.AddYears(1);
        
        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; } = "#ffffff";
        
        [Display(Name = "Text Color")]
        public string TextColor { get; set; } = "#000000";
        
        [Display(Name = "Accent Color")]
        public string AccentColor { get; set; } = "#0284c7";
        
        [Display(Name = "Card Layout")]
        public string Layout { get; set; } = "standard";
        
        [Display(Name = "QR Foreground Color")]
        public string QrFgColor { get; set; } = "#000000";
        
        [Display(Name = "QR Background Color")]
        public string QrBgColor { get; set; } = "#FFFFFF";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Non-persistent properties for form handling
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        
        // Storage for custom template fields data
        [NotMapped]
        public string CustomFieldsData { get; set; } = "{}";
    }
}