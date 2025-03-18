// Path: Models/IssueReport.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class IssueReport
    {
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        public Card Card { get; set; }
        
        [Required]
        [StringLength(50)]
        public string IssueType { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Priority { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ReporterName { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string ReporterEmail { get; set; }
        
        [StringLength(20)]
        public string ReporterPhone { get; set; }
        
        // Removed FollowUp property as requested
        
        public string Status { get; set; } = "Open";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? ResolvedAt { get; set; }
        
        public string Resolution { get; set; }
    }
}