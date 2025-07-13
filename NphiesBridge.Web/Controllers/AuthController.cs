using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Web.Services.API;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already authenticated, redirect to home
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model);

            if (result != null && result.Success)
            {
                // Check if user has Provider role (providers can access this portal)
                if (result.User.Roles.Contains("Provider") || result.User.Roles.Contains("Admin"))
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Access denied. This portal is for healthcare providers only.");
                    _authService.LogoutAsync();
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid email or password.");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            _authService.LogoutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}