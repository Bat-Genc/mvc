using System.ComponentModel.DataAnnotations;

namespace SchoolMvc.Models
{
    public class AppUser
    {
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string CustomPassword { get; set; } = string.Empty;
        
        public string Role { get; set; } = "User";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<UserEncryptionMethod> EncryptionMethods { get; set; } = new List<UserEncryptionMethod>();
        public ICollection<EncryptionHistory> Histories { get; set; } = new List<EncryptionHistory>();
    }
}