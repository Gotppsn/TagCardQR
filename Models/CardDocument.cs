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
        public string Title { get; set; }
        
        [StringLength(50)]
        public string DocumentType { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public string FilePath { get; set; }
        
        [Required]
        public string FileName { get; set; }
        
        public long FileSize { get; set; }
        
        public string FileType { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string UploadedBy { get; set; }
        
        [NotMapped]
        public IFormFile DocumentFile { get; set; }
    }
}