using Microsoft.AspNetCore.Mvc;
using NphiesBridge.AdminPortal.Services.API;
using NphiesBridge.Core.Entities;
using NphiesBridge.Shared.Helpers;
using System.Text.Json;

namespace NphiesBridge.AdminPortal.Controllers
{
    public class HealthProviderController : Controller
    {
        private readonly HealthProviderApiService _apiService;

        public HealthProviderController(HealthProviderApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAllAsync();
            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(HealthProvider model)
        {
            // First, check server-side validation
            if (!ModelState.IsValid)
            {
                return PartialView(model);
            }

            try
            {
                await _apiService.AddAsync(model);

                // Success - return success indicator for AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // DEBUG: Log the actual exception type and message
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Check if it's an API validation error
                if (ex.Message.Contains("BadRequest") && ex.Message.Contains("validation errors"))
                {
                    // Parse API validation errors
                    try
                    {
                        var errorMessage = ex.Message;
                        var startIndex = errorMessage.IndexOf('{');
                        var endIndex = errorMessage.LastIndexOf('}');

                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonPart = errorMessage.Substring(startIndex, endIndex - startIndex + 1);
                            Console.WriteLine("JSON part: " + jsonPart);

                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ApiValidationError>(jsonPart, options);

                            if (errorResponse?.Errors != null)
                            {
                                foreach (var error in errorResponse.Errors)
                                {
                                    foreach (var msg in error.Value)
                                    {
                                        ModelState.AddModelError(error.Key, msg);
                                        Console.WriteLine($"Added error: {error.Key} = {msg}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine("Parse error: " + parseEx.Message);
                        ModelState.AddModelError("", "Validation failed. Please check your input.");
                    }
                }
                else
                {
                    // For testing - add some validation errors
                    ModelState.AddModelError("Email", "Invalid email format (debug)");
                    ModelState.AddModelError("Phone", "Invalid phone format (debug)");
                }

                return PartialView(model);
            }
        }
    

        // EDIT: GET
        public async Task<IActionResult> Edit(Guid id)
        {
            var provider = await _apiService.GetByIdAsync(id);
            if (provider == null) return NotFound();
            return View(provider);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, HealthProvider model)
        {
            // Check if IDs match
            if (id != model.Id)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Invalid request. ID mismatch." });
                }
                return BadRequest("ID mismatch");
            }

            // Check server-side validation
            if (!ModelState.IsValid)
            {
                return PartialView(model);
            }

            try
            {
                await _apiService.UpdateAsync(id, model);

                // Return JSON success for AJAX requests
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Health Provider updated successfully!" });
                }

                // For non-AJAX requests, redirect to index
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // DEBUG: Log the actual exception
                Console.WriteLine($"Edit Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Edit Exception Message: {ex.Message}");

                // Check if it's an API validation error
                if (ex.Message.Contains("BadRequest") && ex.Message.Contains("validation errors"))
                {
                    try
                    {
                        // Parse API validation errors
                        var errorMessage = ex.Message;
                        var startIndex = errorMessage.IndexOf('{');
                        var endIndex = errorMessage.LastIndexOf('}');

                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonPart = errorMessage.Substring(startIndex, endIndex - startIndex + 1);
                            Console.WriteLine("Edit JSON part: " + jsonPart);

                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ApiValidationError>(jsonPart, options);

                            if (errorResponse?.Errors != null)
                            {
                                foreach (var error in errorResponse.Errors)
                                {
                                    foreach (var msg in error.Value)
                                    {
                                        ModelState.AddModelError(error.Key, msg);
                                        Console.WriteLine($"Edit - Added error: {error.Key} = {msg}");
                                    }
                                }
                            }
                        }

                        return PartialView(model);
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine("Edit parse error: " + parseEx.Message);
                        ModelState.AddModelError("", "Validation failed. Please check your input.");
                        return PartialView(model);
                    }
                }
                else if (ex.Message.Contains("not found") || ex.Message.Contains("NotFound"))
                {
                    // Handle not found cases
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Health Provider not found." });
                    }
                    return NotFound("Health Provider not found");
                }
                else
                {
                    // Handle other errors
                    Console.WriteLine($"Edit - General error: {ex.Message}");

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = $"Update failed: {ex.Message}" });
                    }

                    ModelState.AddModelError("", $"Update failed: {ex.Message}");
                    return PartialView(model);
                }
            }
        }


        // DELETE: GET
        public async Task<IActionResult> Delete(Guid id)
        {
            var provider = await _apiService.GetByIdAsync(id);
            if (provider == null) return NotFound();
            return View(provider);
        }

        // DELETE: POST
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _apiService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
