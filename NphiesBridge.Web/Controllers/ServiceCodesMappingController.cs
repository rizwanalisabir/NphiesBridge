using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Web.Services;
using NphiesBridge.Web.Services.API;

namespace NphiesBridge.Web.Controllers
{
    public class ServiceCodesMappingController : Controller
    {
        private readonly ServiceCodesMappingApiService _api;
        private readonly AuthService _auth;
        private readonly ExcelTemplateService _excel;
        private readonly ILogger<ServiceCodesMappingController> _logger;

        public ServiceCodesMappingController(ServiceCodesMappingApiService api, AuthService auth, ExcelTemplateService excel, ILogger<ServiceCodesMappingController> logger)
        {
            _api = api;
            _auth = auth;
            _excel = excel;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ServiceMappingSetup(string? message = null, string? error = null)
        {
            ViewBag.Message = message;
            ViewBag.Error = error;
            return View();
        }

        // Download Service Codes template (like ICD template)
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var bytes = _excel.GenerateServiceCodesTemplate();
                var fileName = $"Service_Codes_Mapping_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Service Codes template");
                TempData["Error"] = "Failed to generate template.";
                return RedirectToAction(nameof(ServiceMappingSetup));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile file, CancellationToken ct)
        {
            if (!_auth.IsAuthenticated())
                return RedirectToAction("Login", "Auth");

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

        [HttpGet]
        public async Task<IActionResult> ServiceMapping(string sessionId, int page = 1, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return RedirectToAction(nameof(ServiceMappingSetup), new { error = "Session not specified" });

            var sessionResp = await _api.GetSessionPageAsync(sessionId, page, 100, ct);
            var statsResp = await _api.GetStatisticsAsync(sessionId, ct);

            ViewBag.Statistics = statsResp?.Data;
            ViewBag.ApiErrors = (sessionResp?.Errors ?? new List<string>()).Concat(statsResp?.Errors ?? new List<string>()).ToList();

            if (sessionResp?.Success == true && sessionResp.Data != null)
                return View(sessionResp.Data);

            return RedirectToAction(nameof(ServiceMappingSetup), new { error = sessionResp?.Errors?.FirstOrDefault() ?? "Unable to load session" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllHigh(string sessionId, int max = 100, CancellationToken ct = default)
        {
            var resp = await _api.ApproveAllHighAsync(sessionId, max, ct);
            var message = resp?.Success == true ? "Approved high-confidence mappings." : (resp?.Errors?.FirstOrDefault() ?? "Failed to approve.");
            TempData["Message"] = message;
            return RedirectToAction(nameof(ServiceMapping), new { sessionId });
        }

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
            TempData["Message"] = resp?.Success == true ? "Mapping saved." : resp?.Errors?.FirstOrDefault() ?? "Failed to save mapping.";
            return RedirectToAction(nameof(ServiceMapping), new { sessionId, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSuggestions(string sessionId, int limit = 100, CancellationToken ct = default)
        {
            var resp = await _api.GenerateSuggestionsAsync(sessionId, limit, ct);
            TempData["Message"] = resp?.Success == true ? "Generated AI suggestions." : resp?.Errors?.FirstOrDefault() ?? "Failed to generate suggestions.";
            return RedirectToAction(nameof(ServiceMapping), new { sessionId });
        }

        // AJAX endpoints (for a smoother experience, similar to icd-mapping.js style)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRowMappingAjax([FromForm] CreateServiceCodeMappingDto dto, [FromForm] string sessionId, CancellationToken ct = default)
        {
            if (dto == null || dto.ProviderServiceItemId == Guid.Empty || string.IsNullOrWhiteSpace(dto.NphiesServiceCode))
                return Json(new { success = false, message = "Invalid mapping data." });

            var resp = await _api.SaveMappingAsync(dto, ct);
            return Json(new { success = resp?.Success == true, message = resp?.Success == true ? "Mapping saved." : resp?.Errors?.FirstOrDefault() ?? "Failed to save mapping." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSuggestionsAjax([FromForm] string sessionId, [FromForm] int limit = 100, CancellationToken ct = default)
        {
            var resp = await _api.GenerateSuggestionsAsync(sessionId, limit, ct);
            return Json(new { success = resp?.Success == true, message = resp?.Message ?? (resp?.Success == true ? "Generated AI suggestions." : "Failed to generate suggestions.") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllHighAjax([FromForm] string sessionId, [FromForm] int max = 100, CancellationToken ct = default)
        {
            var resp = await _api.ApproveAllHighAsync(sessionId, max, ct);
            return Json(new { success = resp?.Success == true, message = resp?.Message ?? (resp?.Success == true ? "Approved." : "Failed to approve.") });
        }
    }
}