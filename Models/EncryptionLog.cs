namespace SchoolMvc.Models
{
    public class EncryptionLog
    {
        public int Id { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string EncryptedText { get; set; } = string.Empty;
        public string PasswordUsed { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}