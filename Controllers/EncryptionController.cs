using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMvc;
using SchoolMvc.Data;
using SchoolMvc.Models;

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
    public async Task<IActionResult> Index(string inputText, string cryptoText, bool decryptMode, IFormFile? uploadFile, string password = "ав", string targetUsername = "everyone", string targetDate = "")
    {
        var model = new EncryptionViewModel();

        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            AppUser? sender = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                sender = await _context.Users.FindAsync(userId);
            }

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

            model.DecryptMode = decryptMode;
            model.OriginalText = decryptMode ? (cryptoText ?? string.Empty) : (inputText ?? string.Empty);
            targetUsername = string.IsNullOrWhiteSpace(targetUsername) ? "everyone" : targetUsername.Trim();

            if (string.IsNullOrWhiteSpace(model.OriginalText))
            {
                model.ErrorMessage = "Моля, въведете текст или качете .txt файл.";
                return View(model);
            }

            var cipher = new CustomCipher();
            var selectedMethodId = HttpContext.Session.GetString("SelectedMethodId");
            var selectedCustomSeed = HttpContext.Session.GetString("SelectedCustomSeed");
            var selectedCustomPassword = HttpContext.Session.GetString("SelectedCustomPassword");

            EncryptionHistory? record = null;
            if (decryptMode)
            {
                record = await _context.EncryptionHistories
                    .FirstOrDefaultAsync(e => e.EncryptedText == model.OriginalText);

                if (record == null)
                {
                    model.ErrorMessage = "Не е намерен запис за този криптографски текст.";
                    return View(model);
                }

                if (!CanDecryptHistoryRecord(record))
                {
                    model.ErrorMessage = "Нямате право да декриптирате това съобщение.";
                    return View(model);
                }
            }

            if (!string.IsNullOrEmpty(selectedMethodId) && !string.IsNullOrEmpty(selectedCustomSeed))
            {
                var customSeed = int.Parse(selectedCustomSeed);
                var customPassword = selectedCustomPassword ?? password;
                var targetCustomPassword = string.Empty;
                if (!string.Equals(targetUsername, "everyone", StringComparison.OrdinalIgnoreCase))
                {
                    if (sender == null)
                    {
                        model.ErrorMessage = "Трябва да сте влезли в системата за криптиране към конкретен потребител.";
                        return View(model);
                    }
                    targetCustomPassword = CustomCipher.GenerateCustomPasswordForUsername(targetUsername);
                    customPassword = sender.CustomPassword + targetCustomPassword + targetUsername;
                }

                if (decryptMode && record != null)
                {
                    var senderUser = await _context.Users.FindAsync(record.UserId);
                    if (senderUser == null)
                    {
                        model.ErrorMessage = "Нямате достъп до изпращача.";
                        return View(model);
                    }
                    var recordTargetCustomPassword = CustomCipher.GenerateCustomPasswordForUsername(record.TargetUsername ?? string.Empty);
                    customPassword = senderUser.CustomPassword + recordTargetCustomPassword + record.TargetUsername;
                }

                if (decryptMode)
                {
                    model.DecryptedText = cipher.DecryptWithPersonalSeed(model.OriginalText, customPassword, customSeed);
                }
                else
                {
                    model.EncryptedText = cipher.EncryptWithPersonalSeed(model.OriginalText, customPassword, customSeed);
                }
            }
            else
            {
                var currentDate = DateTime.UtcNow.AddHours(2);
                var decryptDate = currentDate;
                if (decryptMode && !string.IsNullOrWhiteSpace(targetDate))
                {
                    if (!DateTime.TryParse(targetDate, out decryptDate))
                    {
                        model.ErrorMessage = "Моля, въведете валидна дата за декриптиране.";
                        return View(model);
                    }
                }

                var effectivePassword = password;
                if (!string.Equals(targetUsername, "everyone", StringComparison.OrdinalIgnoreCase))
                {
                    if (sender == null)
                    {
                        model.ErrorMessage = "Трябва да сте влезли в системата за криптиране към конкретен потребител.";
                        return View(model);
                    }
                    var targetCustomPassword = CustomCipher.GenerateCustomPasswordForUsername(targetUsername);
                    effectivePassword = sender.CustomPassword + targetCustomPassword + targetUsername;
                }

                if (decryptMode)
                {
                    if (record != null && !string.Equals(record.TargetUsername, "everyone", StringComparison.OrdinalIgnoreCase))
                    {
                        if (record.UserId == null)
                        {
                            model.ErrorMessage = "Няма информация за изпращача.";
                            return View(model);
                        }
                        var recordSender = await _context.Users.FindAsync(record.UserId.Value);
                        if (recordSender == null)
                        {
                            model.ErrorMessage = "Няма информация за изпращача.";
                            return View(model);
                        }
                        var recordTargetCustomPassword = CustomCipher.GenerateCustomPasswordForUsername(record.TargetUsername ?? string.Empty);
                        effectivePassword = recordSender.CustomPassword + recordTargetCustomPassword + (record.TargetUsername ?? string.Empty);
                    }
                    model.DecryptedText = cipher.DecryptWithDate(model.OriginalText, effectivePassword, decryptDate);
                }
                else
                {
                    model.EncryptedText = cipher.EncryptWithDate(model.OriginalText, effectivePassword, currentDate);
                }
            }

            if (!decryptMode && !string.IsNullOrEmpty(model.EncryptedText))
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                var history = new EncryptionHistory
                {
                    UserId = string.IsNullOrEmpty(userIdString) ? null : int.Parse(userIdString),
                    OriginalText = model.OriginalText,
                    EncryptedText = model.EncryptedText,
                    PasswordUsed = !string.IsNullOrEmpty(selectedMethodId) && !string.IsNullOrEmpty(selectedCustomSeed)
                        ? $"{(selectedCustomPassword ?? password)} (seed {selectedCustomSeed})"
                        : password,
                    EncryptionType = string.IsNullOrEmpty(selectedMethodId) ? "Standard" : "Personal",
                    CreatedAt = DateTime.Now,
                    TargetUsername = targetUsername
                };
                _context.EncryptionHistories.Add(history);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "Грешка при криптиране/декриптиране: " + ex.Message;
        }

        return View(model);
    }

    private bool CanDecryptHistoryRecord(EncryptionHistory record)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var targetUsername = record.TargetUsername?.Trim() ?? string.Empty;

        if (string.Equals(targetUsername, "everyone", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(targetUsername))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(userId) && record.UserId.ToString() == userId)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(userId) && string.Equals(HttpContext.Session.GetString("Username")?.Trim(), targetUsername, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // API за декриптиране (приема JSON)
    // ═══════════════════════════════════════════════════════════════════════════
    [HttpPost]
    public async Task<IActionResult> DecryptWithDate([FromBody] DecryptRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetString("UserId");
            
            var record = await _context.EncryptionHistories
                .FirstOrDefaultAsync(e => e.EncryptedText == request.CryptoText);

            if (record == null)
            {
                return BadRequest("Няма запис за този криптиран текст.");
            }

            bool canDecrypt = false;
            var targetUsername = record.TargetUsername?.Trim() ?? string.Empty;
            if (string.Equals(targetUsername, "everyone", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(targetUsername))
            {
                canDecrypt = true;
            }
            else if (!string.IsNullOrEmpty(userId) && record.UserId.ToString() == userId)
            {
                canDecrypt = true;
            }
            else if (!string.IsNullOrEmpty(userId) && string.Equals(HttpContext.Session.GetString("Username")?.Trim(), targetUsername, StringComparison.OrdinalIgnoreCase))
            {
                canDecrypt = true;
            }

            if (!canDecrypt)
            {
                return BadRequest("Нямате право да декриптирате това съобщение!");
            }

            if (!DateTime.TryParse(request.TargetDate, out var targetDate))
            {
                return BadRequest("Моля, въведете валидна дата за декриптиране.");
            }

            var effectivePassword = request.Password;
            if (!string.Equals(record.TargetUsername?.Trim(), "everyone", StringComparison.OrdinalIgnoreCase))
            {
                if (record.UserId == null)
                {
                    return BadRequest("Няма информация за изпращача.");
                }
                var recordSender = await _context.Users.FindAsync(record.UserId.Value);
                if (recordSender == null)
                {
                    return BadRequest("Няма информация за изпращача.");
                }
                var recordTargetCustomPassword = CustomCipher.GenerateCustomPasswordForUsername(record.TargetUsername ?? string.Empty);
                effectivePassword = recordSender.CustomPassword + recordTargetCustomPassword + (record.TargetUsername?.Trim() ?? string.Empty);
            }

            var cipher = new CustomCipher();
            var decryptedText = cipher.DecryptWithDate(request.CryptoText, effectivePassword, targetDate);

            return Ok(new { success = true, decryptedText });
        }
        catch (Exception ex)
        {
            return BadRequest($"Грешка при декриптиране: {ex.Message}");
        }
    }
}

public class DecryptRequest
{
    public string CryptoText { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TargetDate { get; set; } = string.Empty;
    public string TargetUsername { get; set; } = string.Empty;
}