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
        
        // Compatibility property for Name
        public string Name { 
            get => ProductName; 
            set => ProductName = value; 
        }
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Manufacturer { get; set; } = string.Empty;
        
        // Compatibility property for Company
        public string Company { 
            get => Manufacturer; 
            set => Manufacturer = value; 
        }
        
        [Display(Name = "Model Number")]
        public string ModelNumber { get; set; } = string.Empty;
        
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;
        
        [Display(Name = "Maintenance Information")]
        public string MaintenanceInfo { get; set; } = string.Empty;
        
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
        
        [StringLength(20)]
        public string BackgroundColor { get; set; } = "#ffffff";
        
        [StringLength(20)]
        public string TextColor { get; set; } = "#000000";
        
        [StringLength(20)]
        public string AccentColor { get; set; } = "#0284c7";
        
        [StringLength(20)]
        public string QrFgColor { get; set; } = "#000000";
        
        [StringLength(20)]
        public string QrBgColor { get; set; } = "#FFFFFF";
        
        [Display(Name = "Card Layout")]
        [StringLength(20)]
        public string Layout { get; set; } = "standard";
        
        public string? ImagePath { get; set; }
        
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string CustomFieldsData { get; set; } = "{}";
        
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;
        
        [EmailAddress]
        public string? Email { get; set; }
        
        [StringLength(100)]
        public string UserFullName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string PlantName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}