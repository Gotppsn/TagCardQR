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
        
        public string ImagePath { get; set; } = string.Empty;
        
        [NotMapped]
        public IFormFile ImageFile { get; set; }
        
        // Store custom fields as JSON
        [Column(TypeName = "nvarchar(max)")]
        public string CustomFieldsData { get; set; } = "{}";
        
        // Add CreatedByID property to match Template.cs
        [StringLength(50)]
        public string CreatedByID { get; set; } = string.Empty;
        
        // Add UpdatedByID property
        [StringLength(50)]
        public string UpdatedByID { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public string UpdatedBy { get; set; } = string.Empty;
        
        // New properties for archive and QR code activation
        public bool IsArchived { get; set; } = false;
        public bool IsQrCodeActive { get; set; } = true;
    }
}