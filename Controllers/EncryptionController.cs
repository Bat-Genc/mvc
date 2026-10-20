using Microsoft.AspNetCore.Mvc;
using SchoolMvc;
using SchoolMvc.Models;
using SchoolMvc.Data;

namespace SchoolMvc.Controllers;

public class EncryptionController : Controller
{
    private readonly AppDbContext _context;

    public EncryptionController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(new EncryptionViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string inputText, string cryptoText, bool decryptMode, IFormFile? uploadFile, string password = "ав", string receiverUsername = "anonymous")
    {
        Console.WriteLine($"=== [CRYPTO] ===");
        Console.WriteLine($"receiverUsername: '{receiverUsername}'");
        Console.WriteLine($"password: '{password}'");
        Console.WriteLine($"decryptMode: {decryptMode}");
        Console.WriteLine($"inputText: '{inputText}'");
        Console.WriteLine($"cryptoText: '{cryptoText}'");

        var model = new EncryptionViewModel();

        try
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                using var stream = uploadFile.OpenReadStream();
                using var reader = new StreamReader(stream);
                var uploadedText = await reader.ReadToEndAsync();
                if (decryptMode)
                    cryptoText = uploadedText;
                else
                    inputText = uploadedText;
            }

            model.OriginalText = decryptMode ? (cryptoText ?? string.Empty) : (inputText ?? string.Empty);
            model.DecryptMode = decryptMode;

            if (string.IsNullOrWhiteSpace(model.OriginalText))
            {
                model.ErrorMessage = "Моля, въведете текст или качете .txt файл.";
                return View(model);
            }

            var cipher = new CustomCipher();
            var currentDate = DateTime.UtcNow.AddHours(2);
            var senderUsername = HttpContext.Session.GetString("Username") ?? "anonymous";

            Console.WriteLine($"senderUsername: '{senderUsername}'");
            Console.WriteLine($"currentDate: {currentDate:yyyy-MM-dd HH:mm:ss}");

            var selectedMethodId = HttpContext.Session.GetString("SelectedMethodId");
            var selectedCustomSeed = HttpContext.Session.GetString("SelectedCustomSeed");
            var selectedCustomPassword = HttpContext.Session.GetString("SelectedCustomPassword");

            Console.WriteLine($"selectedMethodId: '{selectedMethodId}'");

            // ✅ АКО Е ИЗБРАН ЛИЧЕН МЕТОД
            if (!string.IsNullOrEmpty(selectedMethodId) && !string.IsNullOrEmpty(selectedCustomSeed))
            {
                var customSeed = int.Parse(selectedCustomSeed);
                var customPassword = selectedCustomPassword ?? password;

                Console.WriteLine($"!!! ЛИЧЕН МЕТОД: customSeed={customSeed}, customPassword='{customPassword}'");

                if (decryptMode)
                {
                    // ✅ Декриптиране с личен метод + метаданни
                    model.DecryptedText = cipher.DecryptWithPersonalSeedAndMetadata(
                        model.OriginalText,
                        customPassword,
                        customSeed,
                        senderUsername
                    );
                }
                else
                {
                    // ✅ Криптиране с личен метод + метаданни
                    model.EncryptedText = cipher.EncryptWithPersonalSeedAndMetadata(
                        model.OriginalText,
                        customPassword,
                        customSeed,
                        senderUsername,
                        receiverUsername
                    );
                }
            }
            else
            {
                // ✅ СТАНДАРТЕН МЕТОД (без личен seed)
                Console.WriteLine($"!!! СТАНДАРТЕН МЕТОД");
                Console.WriteLine($"receiverUsername (в стандартен): '{receiverUsername}'");

                if (decryptMode)
                {
                    Console.WriteLine($"!!! ДЕКРИПТИРАНЕ с receiver: '{senderUsername}'");
                    model.DecryptedText = cipher.DecryptWithMetadata(
                        model.OriginalText,
                        password,
                        currentDate,
                        senderUsername
                    );
                }
                else
                {
                    Console.WriteLine($"!!! КРИПТИРАНЕ за receiver: '{receiverUsername}'");
                    model.EncryptedText = cipher.EncryptWithMetadata(
                        model.OriginalText,
                        password,
                        currentDate,
                        senderUsername,
                        receiverUsername
                    );
                }
            }

            Console.WriteLine($"=== [RESULT] ===");
            Console.WriteLine($"EncryptedText: '{model.EncryptedText}'");
            Console.WriteLine($"DecryptedText: '{model.DecryptedText}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! ГРЕШКА: {ex.Message}");
            model.ErrorMessage = "Грешка: " + ex.Message;
        }

        return View(model);
    }
}