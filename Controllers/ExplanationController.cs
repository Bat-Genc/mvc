using Microsoft.AspNetCore.Mvc;

namespace SchoolMvc.Controllers;

public class ExplanationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
