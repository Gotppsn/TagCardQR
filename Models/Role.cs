using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class Role
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
        
        // Normalization value for case-insensitive comparison
        [Required]
        [StringLength(50)]
        public string NormalizedName { get; set; }
        
        // Concurrency stamp for handling concurrent updates
        [Required]
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        
        // Navigation property for user roles
        public virtual ICollection<UserRole> UserRoles { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}