using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class Card
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Model Number")]
        public string ModelNumber { get; set; } = string.Empty;
        
        [Required]
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
        
        [Display(Name = "Background Color")]
        public string BackgroundColor { get; set; } = "#ffffff";
        
        [Display(Name = "Text Color")]
        public string TextColor { get; set; } = "#000000";
        
        [Display(Name = "Accent Color")]
        public string AccentColor { get; set; } = "#0284c7";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}