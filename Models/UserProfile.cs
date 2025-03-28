// Path: Models/UserProfile.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; }
        
        [StringLength(100)]
        public string Detail_TH_FirstName { get; set; } = ""; // Default empty string
        
        [StringLength(100)]
        public string Detail_TH_LastName { get; set; } = ""; // Default empty string
        
        [StringLength(100)]
        public string Detail_EN_FirstName { get; set; } = ""; // Default empty string
        
        [StringLength(100)]
        public string Detail_EN_LastName { get; set; } = ""; // Default empty string
        
        [EmailAddress]
        [StringLength(255)]
        public string User_Email { get; set; } = ""; // Default empty string
        
        [StringLength(100)]
        public string Plant_Name { get; set; } = ""; // Default empty string
        
        [StringLength(100)]
        public string Department_Name { get; set; } = ""; // Default empty string
        
        [StringLength(50)]
        public string User_Code { get; set; } = ""; // Default empty string
        
        public DateTime FirstLoginAt { get; set; } = DateTime.Now;
        
        public DateTime LastLoginAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
}