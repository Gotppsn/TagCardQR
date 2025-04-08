// Path: Models/IssueStatusUpdateModel.cs
using System.ComponentModel.DataAnnotations;

namespace CardTagManager.Models
{
    public class IssueStatusUpdateModel
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        public string Status { get; set; }
        
        public string Resolution { get; set; }
    }
}