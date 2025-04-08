using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardTagManager.Models
{
    public class DepartmentAccess
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public UserProfile User { get; set; }
        
        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; }
        
        // Added AccessLevel property
        [Required]
        [StringLength(20)]
        public string AccessLevel { get; set; } = "View"; // Default to View access
        
        public DateTime GrantedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string GrantedBy { get; set; }
        
        [StringLength(50)]
        public string GrantedById { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}