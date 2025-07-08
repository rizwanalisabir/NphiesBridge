using Microsoft.AspNetCore.Mvc;
using NphiesBridge.AdminPortal.Services.API;
using NphiesBridge.AdminPortal.Filters;
using NphiesBridge.Shared.DTOs;
using System.Text.Json;
using NphiesBridge.Shared.Helpers;

namespace NphiesBridge.AdminPortal.Controllers
{
    [AdminAuthorize]
    public class UserController : Controller
    {
        private readonly UserApiService _userApiService;

        public UserController(UserApiService userApiService)
        {
            _userApiService = userApiService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userApiService.GetAllAsync();
            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
            ViewBag.Roles = new List<string> { "Admin", "Provider" };
            return PartialView(new CreateUserDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                ViewBag.Roles = new List<string> { "Admin", "Provider" };
                return PartialView(model);
            }

            try
            {
                await _userApiService.AddAsync(model);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User created successfully!" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Handle API validation errors
                if (ex.Message.Contains("BadRequest") && ex.Message.Contains("validation errors"))
                {
                    try
                    {
                        var errorMessage = ex.Message;
                        var startIndex = errorMessage.IndexOf('{');
                        var endIndex = errorMessage.LastIndexOf('}');

                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonPart = errorMessage.Substring(startIndex, endIndex - startIndex + 1);
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ApiValidationError>(jsonPart, options);

                            if (errorResponse?.Errors != null)
                            {
                                foreach (var error in errorResponse.Errors)
                                {
                                    foreach (var msg in error.Value)
                                    {
                                        ModelState.AddModelError(error.Key, msg);
                                    }
                                }
                            }
                        }

                        ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                        ViewBag.Roles = new List<string> { "Admin", "Provider" };
                        return PartialView(model);
                    }
                    catch
                    {
                        ModelState.AddModelError("", "User creation failed. Please check your input.");
                        ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                        ViewBag.Roles = new List<string> { "Admin", "Provider" };
                        return PartialView(model);
                    }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"User creation failed: {ex.Message}" });
                }

                ModelState.AddModelError("", $"User creation failed: {ex.Message}");
                ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                ViewBag.Roles = new List<string> { "Admin", "Provider" };
                return PartialView(model);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userApiService.GetByIdAsync(id);
            if (user == null) return NotFound();

            var updateDto = new UpdateUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                HealthProviderId = user.HealthProviderId,
                IsActive = user.IsActive
            };

            ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
            ViewBag.Roles = new List<string> { "Admin", "Provider" };
            return PartialView(updateDto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, UpdateUserDto model)
        {
            if (id != model.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Invalid request. ID mismatch." });
                }
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                ViewBag.Roles = new List<string> { "Admin", "Provider" };
                return PartialView(model);
            }

            try
            {
                await _userApiService.UpdateAsync(id, model);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User updated successfully!" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Handle API validation errors (same as Create)
                if (ex.Message.Contains("BadRequest") && ex.Message.Contains("validation errors"))
                {
                    // Same error parsing logic as Create method
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"User update failed: {ex.Message}" });
                }

                ModelState.AddModelError("", $"User update failed: {ex.Message}");
                ViewBag.HealthProviders = await _userApiService.GetHealthProvidersAsync();
                ViewBag.Roles = new List<string> { "Admin", "Provider" };
                return PartialView(model);
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userApiService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return PartialView(user);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(UserResponseDto model)
        {
            try
            {
                await _userApiService.DeleteAsync(model.Id);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User deleted successfully!" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"User deletion failed: {ex.Message}" });
                }

                TempData["Error"] = $"User deletion failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}