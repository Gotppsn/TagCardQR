// Path: Models/ScanResult.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
    public class ScanResult
    {
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        public Card Card { get; set; }
        
        [Required]
        public DateTime ScanTime { get; set; } = DateTime.Now;
        
        [StringLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Result { get; set; } = "Success";
        
        [StringLength(50)]
        public string ScannedBy { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string IpAddress { get; set; } = string.Empty;
        
        // Issue tracking enhancements
        [StringLength(100)]
        public string ScanContext { get; set; } = string.Empty;
        
        // Flag to indicate if an issue was detected during this scan
        public bool IssueDetected { get; set; } = false;
        
        // Reference to an issue if this scan led to issue reporting
        public int? RelatedIssueId { get; set; }
        
        [ForeignKey("RelatedIssueId")]
        public IssueReport RelatedIssue { get; set; }
        
        // Metadata to assist with issue categorization
        [StringLength(50)]
        public string ScanCategory { get; set; } = "Regular";
        
        // Boolean flags for quick filtering
        [NotMapped]
        public bool HasIssue => RelatedIssueId.HasValue;
        
        [NotMapped]
        public bool IsRoutineScan => ScanCategory == "Regular";
        
        // Method to create an issue from this scan
        public IssueReport CreateIssueFromScan()
        {
            return new IssueReport
            {
                CardId = this.CardId,
                ReportDate = DateTime.Now,
                Description = $"Issue detected during scan #{Id} on {ScanTime.ToShortDateString()}",
                IssueType = "Scan Detection",
                Priority = "Medium", 
                Status = "Open",
                ReporterName = this.ScannedBy,
                ReporterEmail = string.Empty,
                ReporterPhone = string.Empty
            };
        }
    }
}