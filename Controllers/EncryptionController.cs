using Microsoft.AspNetCore.Mvc;
using SchoolMvc;
using SchoolMvc.Models;

namespace SchoolMvc.Controllers;

public class EncryptionController : Controller
{
    public IActionResult Index()
    {
        return View(new EncryptionViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string inputText, bool decryptMode, IFormFile? uploadFile, string password = "ав")
    {
        var model = new EncryptionViewModel();

        try
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                using var stream = uploadFile.OpenReadStream();
                using var reader = new StreamReader(stream);
                inputText = await reader.ReadToEndAsync();
            }

            model.OriginalText = inputText ?? string.Empty;
            model.DecryptMode = decryptMode;

            if (string.IsNullOrWhiteSpace(model.OriginalText))
            {
                model.ErrorMessage = "Please enter text or upload a .txt file.";
                return View(model);
            }

            var cipher = new CustomCipher();
            if (decryptMode)
            {
                model.DecryptedText = cipher.Decrypt(model.OriginalText, password);
            }
            else
            {
                model.EncryptedText = cipher.Encrypt(model.OriginalText, password);
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "Encryption/Decryption error: " + ex.Message;
        }

        return View(model);
    }
}
