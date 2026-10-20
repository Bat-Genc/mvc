using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolMvc.Models
{
    public class UserEncryptionMethod
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public int CustomSeed { get; set; }
        public string CustomPassword { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // НОВИ КОЛОНИ
        public string? DefaultPassword { get; set; } = null;
        public string? DefaultTargetUsername { get; set; } = "everyone";
        
        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;
    }
}