using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public UserProfile User { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        [ForeignKey("RoleId")]
        public Role Role { get; set; }
        
        // Tracking fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
    }
}