using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMvc;
using SchoolMvc.Data;
using SchoolMvc.Models;
using System.Threading.Tasks;

namespace SchoolMvc.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null || user.PasswordHash != password)
        {
            ViewBag.Error = "Грешно потребителско име или парола";
            return View();
        }

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string email, string password)
    {
        username = username?.Trim() ?? string.Empty;
        email = email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            ViewBag.Error = "Потребителското име вече съществува";
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            ViewBag.Error = "Имейлът вече е регистриран";
            return View();
        }

        var user = new AppUser
        {
            Username = username,
            Email = email,
            PasswordHash = password,
            CustomPassword = CustomCipher.GenerateCustomPasswordForUsername(username),
            Role = "User",
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var pendingCapsulesCount = await _context.EncryptionHistories
            .CountAsync(h => h.TargetUsername.ToLower() == username.ToLower());

        if (pendingCapsulesCount > 0)
        {
            TempData["PendingCapsules"] = $"Добре дошли, {username}! Имате {pendingCapsulesCount} чакащи капсули.";
        }
        else
        {
            TempData["Success"] = "Регистрацията беше успешна. Можете да влезете със своя акаунт.";
        }

        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
        {
            return RedirectToAction("Login");
        }
        
        var user = await _context.Users.FindAsync(userIdInt);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        var methods = _context.UserEncryptionMethods.Where(m => m.UserId == user.Id).ToList();
        
        ViewBag.User = user;
        ViewBag.Methods = methods;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> MyCapsules()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var sentCapsules = await _context.EncryptionHistories
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Include(h => h.User)
            .ToListAsync();

        var incomingCapsules = await _context.EncryptionHistories
            .Where(h => h.TargetUsername == user.Username && h.UserId != user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Include(h => h.User)
            .ToListAsync();

        var model = new MyCapsulesViewModel
        {
            User = user,
            SentCapsules = sentCapsules,
            IncomingCapsules = incomingCapsules
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string username, string email, string newPassword)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }
        
        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user != null)
        {
            var oldUsername = user.Username;
            user.Username = username;
            user.Email = email;
            user.CustomPassword = CustomCipher.GenerateCustomPasswordForUsername(username);
            if (!string.IsNullOrEmpty(newPassword))
            {
                user.PasswordHash = newPassword;
            }
            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("Username", username);
            TempData["Success"] = "Профилът беше обновен успешно!";
        }
        
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult AddEncryptionMethod()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddEncryptionMethod(string methodName, int customSeed, string customPassword, 
        string? defaultPassword, string? defaultTargetUsername)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }
        
        var method = new UserEncryptionMethod
        {
            UserId = int.Parse(userId),
            MethodName = methodName,
            CustomSeed = customSeed,
            CustomPassword = customPassword,
            IsActive = true,
            CreatedAt = DateTime.Now,
            DefaultPassword = string.IsNullOrWhiteSpace(defaultPassword) ? null : defaultPassword,
            DefaultTargetUsername = string.IsNullOrWhiteSpace(defaultTargetUsername) ? "everyone" : defaultTargetUsername
        };
        
        _context.UserEncryptionMethods.Add(method);
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> DeleteEncryptionMethod(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }
        
        var method = await _context.UserEncryptionMethods
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == int.Parse(userId));
        
        if (method != null)
        {
            _context.UserEncryptionMethods.Remove(method);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Методът е изтрит успешно!";
        }
        
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> UseEncryptionMethod(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }
        
        var method = await _context.UserEncryptionMethods
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == int.Parse(userId));
        
        if (method == null)
        {
            TempData["Error"] = "Методът не е намерен!";
            return RedirectToAction("Profile");
        }
        
        // Запази основната информация
        HttpContext.Session.SetString("SelectedMethodId", method.Id.ToString());
        HttpContext.Session.SetString("SelectedMethodName", method.MethodName);
        HttpContext.Session.SetString("SelectedCustomSeed", method.CustomSeed.ToString());
        HttpContext.Session.SetString("SelectedCustomPassword", method.CustomPassword);
        
        // Запази паролата по подразбиране (ако има)
        if (!string.IsNullOrEmpty(method.DefaultPassword))
        {
            HttpContext.Session.SetString("SelectedDefaultPassword", method.DefaultPassword);
        }
        else
        {
            HttpContext.Session.Remove("SelectedDefaultPassword");
        }
        
        // Запази получателя по подразбиране
        HttpContext.Session.SetString("SelectedDefaultTargetUsername", method.DefaultTargetUsername ?? "everyone");
        
        TempData["Success"] = $"Избран е метод: {method.MethodName}";
        return RedirectToAction("Index", "Encryption");
    }

    [HttpGet]
    public IActionResult ClearSelectedMethod()
    {
        HttpContext.Session.Remove("SelectedMethodId");
        HttpContext.Session.Remove("SelectedMethodName");
        HttpContext.Session.Remove("SelectedCustomSeed");
        HttpContext.Session.Remove("SelectedCustomPassword");
        HttpContext.Session.Remove("SelectedDefaultPassword");
        HttpContext.Session.Remove("SelectedDefaultTargetUsername");
        
        TempData["Success"] = "Избраният метод е премахнат.";
        return RedirectToAction("Index", "Encryption");
    }
}