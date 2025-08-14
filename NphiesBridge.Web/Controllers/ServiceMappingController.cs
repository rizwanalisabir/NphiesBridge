using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.Helpers;
using NphiesBridge.Web.Filters;
using NphiesBridge.Web.Services;
using NphiesBridge.Web.Services.API;
using ClosedXML.Excel;

namespace NphiesBridge.Web.Controllers
{
    [ProviderAuthorize]
    public class ServiceMappingController : Controller
    {
        private readonly ExcelTemplateService _excelService;
        private readonly ServiceMappingApiService _serviceApi;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceMappingController> _logger;
        private readonly AuthService _authService;

        public ServiceMappingController(
            ExcelTemplateService excelService,
            ServiceMappingApiService serviceApi,
            IConfiguration configuration,
            ILogger<ServiceMappingController> logger,
            AuthService authService)
        {
            _excelService = excelService;
            _serviceApi = serviceApi;
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }

        // Landing page for mapping UI (data loaded via JS using sessionId)
        public IActionResult Index()
        {
            return View();
        }

        // Setup wizard page (download/upload)
        public IActionResult MappingSetup()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templateBytes = _excelService.GenerateServiceCodesTemplate();
                var fileName = $"Service_Codes_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service codes template");
                TempData["Error"] = $"Error generating template: {ex.Message}";
                return RedirectToAction("MappingSetup");
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

                // Validate and parse the Service Codes Excel (expects headers from the provided template)
                var parsed = ParseServiceCodesExcel(templateFile);
                if (!parsed.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Template validation failed",
                        errors = parsed.Errors,
                        totalRows = parsed.TotalRows,
                        errorRows = parsed.ErrorRowCount
                    });
                }

                // Build create-session request
                var requestDto = new CreateServiceSessionRequestDto
                {
                    HealthProviderId = LoggedInUserHelper.GetCurrentHealthProviderId(HttpContext),
                    FileName = templateFile.FileName,
                    HealthProviderServiceCodes = parsed.ValidRows
                };

                var sessionResponse = await _serviceApi.CreateMappingSessionAsync(requestDto);

                if (sessionResponse?.Success != true || sessionResponse.Data == null || string.IsNullOrWhiteSpace(sessionResponse.Data.SessionId))
                {
                    return Json(new
                    {
                        success = false,
                        message = sessionResponse?.Message ?? "Failed to create mapping session"
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Template uploaded and validated successfully!",
                    sessionId = sessionResponse.Data.SessionId,
                    totalRows = sessionResponse.Data.TotalRows,
                    validRows = parsed.ValidRowCount,
                    errorRows = parsed.ErrorRowCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded service codes template");
                return Json(new
                {
                    success = false,
                    message = $"Error processing file: {ex.Message}"
                });
            }
        }

        public IActionResult StartMapping(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    TempData["Error"] = "No session found. Please upload your template first.";
                    return RedirectToAction("MappingSetup");
                }

                // Redirect to service mapping page - data will be loaded via API/JS
                return Redirect($"/ServiceMapping?sessionId={sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service mapping process: {Error}", ex.Message);
                TempData["Error"] = "Error starting mapping process.";
                return RedirectToAction("MappingSetup");
            }
        }

        [HttpGet]
        [Route("ServiceMapping")]
        public async Task<IActionResult> ServiceCodesMapping(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    TempData["Error"] = "No mapping session found. Please upload your template first.";
                    return RedirectToAction("MappingSetup");
                }

                // Load session data via API service (for server-rendered model; JS also fetches)
                var sessionResponse = await _serviceApi.GetMappingSessionAsync(sessionId);

                if (sessionResponse?.Success != true || sessionResponse.Data == null)
                {
                    TempData["Error"] = sessionResponse?.Message ?? "Failed to load mapping session data.";
                    return RedirectToAction("MappingSetup");
                }

                var model = sessionResponse.Data;
                ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                return View("ServiceMapping", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service mapping page");
                TempData["Error"] = "Error loading mapping page. Please try again.";
                return RedirectToAction("MappingSetup");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMapping([FromBody] SaveServiceMappingRequest request)
        {
            try
            {
                request.HealthProviderId = LoggedInUserHelper.GetCurrentHealthProviderId(HttpContext);
                request.MappedBy = _authService.GetCurrentUser().Id;

                var result = await _serviceApi.SaveMappingAsync(request);

                if (result?.Success == true)
                {
                    return Json(new { success = true, message = result.Message, data = result.Data });
                }

                return Json(new { success = false, message = result?.Message ?? "Failed to save mapping." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service mapping in web controller");
                return Json(new { success = false, message = "An error occurred while saving the mapping" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveBulkMappings([FromBody] List<SaveServiceMappingRequest> requests)
        {
            try
            {
                var healthProviderId = LoggedInUserHelper.GetCurrentHealthProviderId(HttpContext);
                var userId = _authService.GetCurrentUser().Id;

                // Set HealthProviderId and MappedBy for each mapping
                foreach (var req in requests)
                {
                    req.HealthProviderId = healthProviderId;
                    req.MappedBy = userId;
                }

                // Call SaveMapping for each request, collect results
                var results = new List<object>();
                foreach (var req in requests)
                {
                    var result = await _serviceApi.SaveMappingAsync(req);
                    results.Add(new
                    {
                        input = req,
                        success = result?.Success == true,
                        message = result?.Message,
                        data = result?.Data
                    });
                }

                // Return all results
                return Json(new
                {
                    success = true,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bulk service mappings in web controller");
                return Json(new { success = false, message = "An error occurred while saving bulk mappings" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkMatch([FromBody] BulkServiceMatchingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return Json(new { success = false, message = "Session ID is required." });
                }

                var result = await _serviceApi.BulkMatchAsync(request);

                if (result?.Success == true)
                {
                    return Json(new { success = true, message = result.Message, data = result.Data });
                }

                return Json(new { success = false, message = result?.Message ?? "Bulk match failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk match for session: {SessionId}", request.SessionId);
                return Json(new { success = false, message = "An error occurred during bulk matching." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportMappings([FromBody] ExportServiceMappingsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return BadRequest("Session ID is required");
                }

                // Defaults for export request (if not set by client)
                request.IncludeUnmapped = request.IncludeUnmapped;
                request.Format = string.IsNullOrWhiteSpace(request.Format) ? "xlsx" : request.Format;

                var fileData = await _serviceApi.ExportMappingsAsync(request);

                if (fileData != null)
                {
                    var fileName = $"Service_Mappings_{request.SessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

                return Json(new { success = false, message = "Failed to export mappings." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting service mappings for session: {SessionId}", request.SessionId);
                return Json(new { success = false, message = "Error exporting mappings." });
            }
        }

        // Helper: Parse the uploaded Service Codes Excel file to ExcelServiceImportDto list
        private (bool IsValid, int TotalRows, int ValidRowCount, int ErrorRowCount, List<string> Errors, List<ExcelServiceImportDto> ValidRows) ParseServiceCodesExcel(IFormFile file)
        {
            var errors = new List<string>();
            var valid = new List<ExcelServiceImportDto>();
            int totalRows = 0;

            try
            {
                // Size check (10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    errors.Add("File size exceeds 10MB limit");
                    return (false, 0, 0, 0, errors, valid);
                }

                // Extension check
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".xls")
                {
                    errors.Add("Only .xlsx and .xls files are supported");
                    return (false, 0, 0, 0, errors, valid);
                }

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheets.FirstOrDefault();
                if (sheet == null)
                {
                    errors.Add("No worksheets found in the file");
                    return (false, 0, 0, 0, errors, valid);
                }

                // Expect template headers: ItemId | ItemRelation | Name | NPHIESCode | NPHIESDescription
                var expectedHeaders = new[] { "ItemId", "ItemRelation", "Name", "NPHIESCode", "NPHIESDescription" };
                var actualHeaders = new string[expectedHeaders.Length];

                for (int i = 1; i <= expectedHeaders.Length; i++)
                {
                    actualHeaders[i - 1] = sheet.Cell(1, i).GetValue<string>().Trim();
                }

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    if (!string.Equals(expectedHeaders[i], actualHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Column {i + 1} should be '{expectedHeaders[i]}' but found '{actualHeaders[i]}'");
                    }
                }

                if (errors.Any())
                {
                    return (false, 0, 0, 0, errors, valid);
                }

                var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
                totalRows = Math.Max(0, lastRow - 1);

                if (totalRows == 0)
                {
                    errors.Add("No data rows found. Please add your service codes to the template.");
                    return (false, 0, 0, 0, errors, valid);
                }

                if (totalRows > 5000)
                {
                    errors.Add("Maximum 5000 rows allowed. Please split your data into smaller files.");
                    return (false, 0, 0, 0, errors, valid);
                }

                // Parse rows
                var relations = new HashSet<string>(); // ItemRelation should be unique per template row ideally
                for (int row = 2; row <= lastRow; row++)
                {
                    var itemId = sheet.Cell(row, 1).GetValue<string>().Trim();
                    var itemRelation = sheet.Cell(row, 2).GetValue<string>().Trim();
                    var name = sheet.Cell(row, 3).GetValue<string>().Trim();
                    var nphiesCode = sheet.Cell(row, 4).GetValue<string>().Trim();
                    // var nphiesDescription = sheet.Cell(row, 5).GetValue<string>().Trim(); // optional, not used in request

                    // Required field(s)
                    if (string.IsNullOrEmpty(itemRelation))
                    {
                        errors.Add($"Row {row}: ItemRelation is required");
                        continue;
                    }

                    // Optional: warn on missing Name but allow
                    // Optional: detect duplicate ItemRelation within the file
                    if (relations.Contains(itemRelation))
                    {
                        errors.Add($"Row {row}: Duplicate ItemRelation '{itemRelation}'");
                        continue;
                    }
                    relations.Add(itemRelation);

                    valid.Add(new ExcelServiceImportDto
                    {
                        HealthProviderServiceId = itemId ?? string.Empty,
                        HealthProviderServiceRelation = itemRelation,
                        HealthProviderServiceName = name ?? string.Empty,
                        NphiesServiceCode = nphiesCode ?? string.Empty
                    });
                }

                var validCount = valid.Count;
                var errorCount = totalRows - validCount;
                var isValid = !errors.Any() && validCount > 0;

                return (isValid, totalRows, validCount, errorCount, errors, valid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing service codes Excel");
                errors.Add($"Error reading file: {ex.Message}");
                return (false, totalRows, valid.Count, Math.Max(0, totalRows - valid.Count), errors, valid);
            }
        }
    }
}