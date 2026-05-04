using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            // If already logged in, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
