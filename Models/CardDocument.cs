// Path: Models/CardDocument.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace CardTagManager.Models
{
    public class CardDocument
    {
        public int Id { get; set; }
        
        [Required]
        public int CardId { get; set; }
        
        [ForeignKey("CardId")]
        public Card Card { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public string FileType { get; set; } = string.Empty;
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string UploadedBy { get; set; } = string.Empty;
        
        [NotMapped]
        public IFormFile DocumentFile { get; set; }
    }
}