namespace SchoolMvc.Models;

public class EncryptionViewModel
{
    public string OriginalText { get; set; } = string.Empty;
    public string EncryptedText { get; set; } = string.Empty;
    public string DecryptedText { get; set; } = string.Empty;
    public bool DecryptMode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
