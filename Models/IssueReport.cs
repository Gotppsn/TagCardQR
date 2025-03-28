// Path: Models/IssueReport.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace CardTagManager.Models
{
    public class IssueReport
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Card ID is required")]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        public Card? Card { get; set; }
        
        [Required(ErrorMessage = "Issue Type is required")]
        [StringLength(50)]
        public string IssueType { get; set; } = "Device Malfunction";
        
        [Required(ErrorMessage = "Priority is required")]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Report Date is required")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; } = DateTime.Now;
        
        [Required(ErrorMessage = "Reporter Name is required")]
        [StringLength(100)]
        public string ReporterName { get; set; } = "Anonymous";
        
        [Required(ErrorMessage = "Reporter Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string ReporterEmail { get; set; } = "anonymous@example.com";
        
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? ReporterPhone { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Open";
        
        public string Resolution { get; set; } = string.Empty;
        
        // Image path to store the uploaded image
        [StringLength(500)]
        public string? ImagePath { get; set; }
        
        // The actual file uploaded (not stored in DB)
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
    }
}