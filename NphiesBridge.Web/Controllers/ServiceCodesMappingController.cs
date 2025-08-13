using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Web.Services.API;

namespace NphiesBridge.Web.Controllers
{
    public class ServiceCodesMappingController : Controller
    {
        private readonly ServiceCodesMappingApiService _api;
        private readonly AuthService _auth;

        public ServiceCodesMappingController(ServiceCodesMappingApiService api, AuthService auth)
        {
            _api = api;
            _auth = auth;
        }

        // GET: setup/upload page
        [HttpGet]
        public IActionResult ServiceMappingSetup(string? message = null, string? error = null)
        {
            ViewBag.Message = message;
            ViewBag.Error = error;
            return View();
        }

        // POST: handle Excel upload -> creates session then redirect to mapping view
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile file, CancellationToken ct)
        {
            if (!_auth.IsAuthenticated())
            {
                TempData["Error"] = "You must be logged in.";
                return RedirectToAction("Login", "Auth");
            }

            var user = _auth.GetCurrentUser();
            if (user?.HealthProviderId == null || user.HealthProviderId == Guid.Empty)
            {
                TempData["Error"] = "Your account is not linked to a Health Provider.";
                return RedirectToAction(nameof(ServiceMappingSetup));
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a valid Excel file.";
                return RedirectToAction(nameof(ServiceMappingSetup));
            }

            var apiResponse = await _api.CreateSessionViaExcelAsync(user.HealthProviderId.Value, file, ct);
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return RedirectToAction(nameof(ServiceMapping), new { sessionId = apiResponse.Data.SessionId });
            }

            TempData["Error"] = apiResponse?.Errors?.FirstOrDefault() ?? "Failed to create session.";
            return RedirectToAction(nameof(ServiceMappingSetup));
        }

        // GET: mapping page for a session (first 100 items)
        [HttpGet]
        public async Task<IActionResult> ServiceMapping(string sessionId, int page = 1, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return RedirectToAction(nameof(ServiceMappingSetup), new { error = "Session not specified" });
            }

            var sessionResp = await _api.GetSessionPageAsync(sessionId, page, 100, ct);
            var statsResp = await _api.GetStatisticsAsync(sessionId, ct);

            ViewBag.Statistics = statsResp?.Data;
            ViewBag.ApiErrors = (sessionResp?.Errors ?? new List<string>()).Concat(statsResp?.Errors ?? new List<string>()).ToList();

            if (sessionResp?.Success == true && sessionResp.Data != null)
            {
                return View(sessionResp.Data);
            }

            return RedirectToAction(nameof(ServiceMappingSetup), new { error = sessionResp?.Errors?.FirstOrDefault() ?? "Unable to load session" });
        }

        // POST: approve all high-confidence suggestions (up to 100)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllHigh(string sessionId, int max = 100, CancellationToken ct = default)
        {
            var resp = await _api.ApproveAllHighAsync(sessionId, max, ct);
            var message = resp?.Success == true ? "Approved high-confidence mappings." : (resp?.Errors?.FirstOrDefault() ?? "Failed to approve.");
            return RedirectToAction(nameof(ServiceMapping), new { sessionId, message });
        }

        // POST: save a single mapping from the grid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRowMapping(CreateServiceCodeMappingDto dto, string sessionId, int page = 1, CancellationToken ct = default)
        {
            if (dto == null || dto.ProviderServiceItemId == Guid.Empty || string.IsNullOrWhiteSpace(dto.NphiesServiceCode))
            {
                TempData["Error"] = "Invalid mapping data.";
                return RedirectToAction(nameof(ServiceMapping), new { sessionId, page });
            }

            var resp = await _api.SaveMappingAsync(dto, ct);
            if (resp?.Success == true)
                TempData["Message"] = "Mapping saved.";
            else
                TempData["Error"] = resp?.Errors?.FirstOrDefault() ?? "Failed to save mapping.";

            return RedirectToAction(nameof(ServiceMapping), new { sessionId, page });
        }
    }
}