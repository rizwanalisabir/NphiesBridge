using Microsoft.AspNetCore.Mvc;
using NphiesBridge.AdminPortal.Services.API;
using NphiesBridge.AdminPortal.ViewModels;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.AdminPortal.Controllers
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
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var loginDto = new LoginDto
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var result = await _authService.LoginAsync(loginDto);

            if (result != null && result.Success)
            {
                // Check if user has Admin role
                if (!_authService.IsInRole("Admin"))
                {
                    await _authService.LogoutAsync();
                    ModelState.AddModelError(string.Empty, "Access denied. Admin privileges required.");
                    return View(model);
                }

                // Redirect to return URL or home
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}