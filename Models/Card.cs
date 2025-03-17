// Path: Models/Card.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;


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
        
        // LDAP populated fields marked as ReadOnly
        [ReadOnly(true)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
        
        [ReadOnly(true)]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;
        
        [ReadOnly(true)]
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [ReadOnly(true)]
        [Display(Name = "User's Name")]
        public string UserFullName { get; set; } = string.Empty;
        
        [ReadOnly(true)]
        [Display(Name = "Plant Name")]
        public string PlantName { get; set; } = string.Empty;
        
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
        
        public string ImagePath { get; set; }
        
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
        
        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; } = "#ffffff";
        
        [Display(Name = "Text Color")]
        public string TextColor { get; set; } = "#000000";
        
        [Display(Name = "Accent Color")]
        public string AccentColor { get; set; } = "#0284c7";
        
        // Added to store card layout selection
        [Display(Name = "Card Layout")]
        public string Layout { get; set; } = "standard";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}