using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolMvc.Models
{
    public class EncryptionHistory
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string EncryptedText { get; set; } = string.Empty;
        public string PasswordUsed { get; set; } = string.Empty;
        public string EncryptionType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // НОВОТО ПРОПЪРТИ
        public string TargetUsername { get; set; } = "everyone";
        
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }
    }
}