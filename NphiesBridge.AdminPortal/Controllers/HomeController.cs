using Microsoft.AspNetCore.Mvc;
using NphiesBridge.AdminPortal.Services.API;

namespace NphiesBridge.AdminPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly AuthService _authService;

        public HomeController(AuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Index()
        {
            // Check if user is authenticated
            if (!_authService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check if user has Admin role
            if (!_authService.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // User is authenticated and authorized - show dashboard
            return View();
        }
    }
}