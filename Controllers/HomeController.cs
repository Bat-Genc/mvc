using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SchoolMvc.Models;
using SchoolMvc;

namespace SchoolMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new Models.EncryptionViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string inputText, IFormFile? uploadFile)
    {
        var model = new Models.EncryptionViewModel();

        try
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                using var stream = uploadFile.OpenReadStream();
                using var reader = new StreamReader(stream);
                inputText = await reader.ReadToEndAsync();
            }

            model.OriginalText = inputText ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.OriginalText))
            {
                model.ErrorMessage = "Please enter text or upload a .txt file.";
                return View(model);
            }

            var cipher = new CustomCipher();
            model.EncryptedText = cipher.EncryptWithDate(model.OriginalText, "ав", DateTime.UtcNow.AddHours(2));
            model.DecryptedText = cipher.DecryptWithDate(model.EncryptedText, "ав", DateTime.UtcNow.AddHours(2));
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "Error encrypting/decrypting: " + ex.Message;
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
