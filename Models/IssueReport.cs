// Path: Models/IssueReport.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
public class IssueReport
{
    public int Id { get; set; }
    
    [Required]
    public int CardId { get; set; }
    
    [ForeignKey("CardId")]
    public Card Card { get; set; }
    
    [Required]
    [StringLength(50)]
    public string IssueType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Date)]
    public DateTime ReportDate { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ReporterName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string ReporterEmail { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string ReporterPhone { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Open";
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? ResolvedAt { get; set; }
    
    public string Resolution { get; set; } = string.Empty;
}
}