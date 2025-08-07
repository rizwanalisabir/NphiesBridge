using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.DTOs.NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.Helpers;
using NphiesBridge.Web.Filters;
using NphiesBridge.Web.Services;
using NphiesBridge.Web.Services.API;

namespace NphiesBridge.Web.Controllers
{
    [ProviderAuthorize]
    public class IcdMappingController : Controller
    {
        private readonly ExcelTemplateService _excelService;
        private readonly IcdMappingApiService _mappingApiService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IcdMappingController> _logger;
        private readonly AuthService _authService;

        public IcdMappingController(
            ExcelTemplateService excelService,
            IcdMappingApiService mappingApiService,
            IConfiguration configuration,
            ILogger<IcdMappingController> logger,
            AuthService authService)
        {
            _excelService = excelService;
            _mappingApiService = mappingApiService;
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SetupAustralianModification()
        {
            // User selected Australian Modification - no mapping needed
            // Store their choice and redirect to success page
            return View("AustralianModificationSuccess");
        }

        public IActionResult SetupCustomMapping()
        {
            // User selected other system - needs mapping
            return View("MappingSetup");
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templateBytes = _excelService.GenerateIcdMappingTemplate();
                var fileName = $"ICD_Mapping_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template");
                TempData["Error"] = $"Error generating template: {ex.Message}";
                return RedirectToAction("SetupCustomMapping");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadTemplate(IFormFile templateFile)
        {
            try
            {
                if (templateFile == null || templateFile.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                // Step 1: Validate the template using Excel service
                var validationResult = _excelService.ValidateTemplate(templateFile);

                if (!validationResult.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Template validation failed",
                        errors = validationResult.Errors,
                        totalRows = validationResult.TotalRows,
                        errorCount = validationResult.ErrorRowCount
                    });
                }

                // Step 2: Create session via API service (proper service layer)
                var sessionResponse = await CreateMappingSessionViaService(validationResult.ValidRows, templateFile.FileName);

                if (sessionResponse?.Success != true)
                {
                    return Json(new
                    {
                        success = false,
                        message = sessionResponse?.Message ?? "Failed to create mapping session"
                    });
                }

                // Step 3: Return success with session ID
                return Json(new
                {
                    success = true,
                    message = "Template uploaded and validated successfully!",
                    sessionId = sessionResponse.Data?.SessionId,
                    totalRows = validationResult.TotalRows,
                    validRows = validationResult.ValidRowCount,
                    errorRows = validationResult.ErrorRowCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded template");
                return Json(new
                {
                    success = false,
                    message = $"Error processing file: {ex.Message}"
                });
            }
        }

        private async Task<ApiResponse<CreateSessionResponseDto>?> CreateMappingSessionViaService(
            List<ExcelIcdImportDto> validRows,
            string fileName)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString();

                var requestDto = new CreateSessionRequestDto
                {
                    SessionId = sessionId,
                    HealthProviderId = LoggedInUserHelper.GetCurrentHealthProviderId(HttpContext),
                    FileName = fileName,
                    HospitalCodes = validRows.Select(row => new UploadedHospitalCodeDto
                    {
                        HospitalCode = row.HospitalCode ?? "",
                        DiagnosisName = row.DiagnosisName ?? "",
                        DiagnosisDescription = row.DiagnosisDescription,
                        SuggestedIcd10Am = row.Icd10AmCode
                    }).ToList()
                };

                // Use API service instead of direct HttpClient
                return await _mappingApiService.CreateMappingSessionAsync(requestDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mapping session via service");
                return ApiResponse<CreateSessionResponseDto>.ErrorResult($"Service error: {ex.Message}");
            }
        }

        //private Guid GetCurrentHealthProviderId()
        //{
        //    try
        //    {
        //        // Get from session, claims, or user context
        //        var userSession = HttpContext.Session.GetString("ProviderCurrentUser");
        //        if (Guid.TryParse(userSession, out Guid providerId))
        //        {
        //            return providerId;
        //        }

        //        // Try to get from user claims (if using JWT/Identity)
        //        var userIdClaim = User.FindFirst("HealthProviderId")?.Value;
        //        if (Guid.TryParse(userIdClaim, out Guid claimProviderId))
        //        {
        //            return claimProviderId;
        //        }

        //        // For development/testing - use a default provider ID
        //        // In production, this should never happen if auth is properly set up
        //        _logger.LogWarning("No HealthProviderId found in session or claims, using default");
        //        return Guid.Parse("00000000-0000-0000-0000-000000000001"); // Replace with actual default logic
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting current health provider ID");
        //        throw new InvalidOperationException("Health Provider ID not found in session", ex);
        //    }
        //}

        public IActionResult StartMapping(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    TempData["Error"] = "No session found. Please upload your template first.";
                    return RedirectToAction("SetupCustomMapping");
                }

                // Redirect to mapping page - data will be loaded via API
                return Redirect($"/IcdMapping?sessionId={sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting mapping process: {Error}", ex.Message);
                TempData["Error"] = "Error starting mapping process.";
                return RedirectToAction("SetupCustomMapping");
            }
        }

        [HttpGet]
        [Route("IcdMapping")]
        public async Task<IActionResult> ICD10Mapping(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    TempData["Error"] = "No mapping session found. Please upload your template first.";
                    return RedirectToAction("SetupCustomMapping");
                }

                // Load session data via API service
                var sessionResponse = await _mappingApiService.GetMappingSessionAsync(sessionId);

                if (sessionResponse?.Success != true || sessionResponse.Data == null)
                {
                    TempData["Error"] = sessionResponse?.Message ?? "Failed to load mapping session data.";
                    return RedirectToAction("SetupCustomMapping");
                }

                // Pass the actual data to the view
                var model = sessionResponse.Data;
                ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                return View("ICD10Mapping", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ICD mapping page");
                TempData["Error"] = "Error loading mapping page. Please try again.";
                return RedirectToAction("SetupCustomMapping");
            }
        }

        public IActionResult UploadSuccess()
        {
            // Get upload statistics from TempData
            var uploadStatsJson = TempData["UploadStats"] as string;
            if (string.IsNullOrEmpty(uploadStatsJson))
            {
                return RedirectToAction("SetupCustomMapping");
            }

            var uploadStats = System.Text.Json.JsonSerializer.Deserialize<dynamic>(uploadStatsJson);
            ViewBag.UploadStats = uploadStats;

            return View();
        }

        // Additional helper methods using API service
        public async Task<IActionResult> MappingResults(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    TempData["Error"] = "Session ID is required.";
                    return RedirectToAction("SetupCustomMapping");
                }

                var statistics = await _mappingApiService.GetMappingStatisticsAsync(sessionId);

                if (statistics?.Success == true)
                {
                    ViewBag.Statistics = statistics.Data;
                    ViewBag.SessionId = sessionId;
                    return View();
                }

                TempData["Error"] = statistics?.Message ?? "Unable to load mapping results.";
                return RedirectToAction("SetupCustomMapping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mapping results for session: {SessionId}", sessionId);
                TempData["Error"] = "Error loading results.";
                return RedirectToAction("SetupCustomMapping");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportMappings(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("Session ID is required");
                }

                var exportRequest = new ExportMappingsRequestDto
                {
                    SessionId = sessionId,
                    IncludeUnapproved = true
                };

                var fileData = await _mappingApiService.ExportMappingsAsync(exportRequest);

                if (fileData != null)
                {
                    var fileName = $"ICD_Mappings_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

                TempData["Error"] = "Failed to export mappings.";
                return RedirectToAction("MappingResults", new { sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting mappings for session: {SessionId}", sessionId);
                TempData["Error"] = "Error exporting mappings.";
                return RedirectToAction("MappingResults", new { sessionId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            try
            {
                request.HealthProviderId = LoggedInUserHelper.GetCurrentHealthProviderId(HttpContext);
                request.MappedBy = _authService.GetCurrentUser().Id;
                var result = await _mappingApiService.SaveMappingAsync(request);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message, data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving mapping in web controller");
                return Json(new { success = false, message = "An error occurred while saving the mapping" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveBulkMappings([FromBody] SaveBulkMappingsRequest request)
        {
            try
            {
                var result = await _mappingApiService.SaveBulkMappingsAsync(request);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message, data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bulk mappings in web controller");
                return Json(new { success = false, message = "An error occurred while saving bulk mappings" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckExistingMappings(Guid sessionId)
        {
            try
            {
                var result = await _mappingApiService.CheckExistingMappingsAsync(sessionId);

                if (result.Success)
                {
                    return Json(new { success = true, data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing mappings in web controller");
                return Json(new { success = false, message = "An error occurred while checking existing mappings" });
            }
        }
    }
}