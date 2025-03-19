// Path: Models/Template.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CardTagManager.Models
{
    public class Template
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Icon { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string BgColor { get; set; } = "#f0f9ff";
        
        // Store fields as JSON
        [Column(TypeName = "nvarchar(max)")]
        public string FieldsJson { get; set; } = string.Empty;
        
        [NotMapped]
        public List<TemplateField> Fields
        {
            get => !string.IsNullOrEmpty(FieldsJson) 
                  ? JsonSerializer.Deserialize<List<TemplateField>>(FieldsJson) 
                  : new List<TemplateField>();
            set => FieldsJson = JsonSerializer.Serialize(value);
        }
        
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
    
    public class TemplateField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string Icon { get; set; } = "tag";
        public string Placeholder { get; set; } = string.Empty;
        public bool Required { get; set; } = true;
        public List<string> Options { get; set; } = new List<string>();
    }
}