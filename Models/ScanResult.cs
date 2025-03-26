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
    }
}