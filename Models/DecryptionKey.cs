using System.ComponentModel.DataAnnotations;

namespace SchoolMvc.Models
{
    public class DecryptionKey
    {
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int TargetId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SenderCustomPassword { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string TargetCustomPassword { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string TargetUsername { get; set; } = string.Empty;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        public string EncryptionType { get; set; } = "date"; // or "personal"
        
        // Navigation
        public AppUser Sender { get; set; } = null!;
        public AppUser Target { get; set; } = null!;
    }
}