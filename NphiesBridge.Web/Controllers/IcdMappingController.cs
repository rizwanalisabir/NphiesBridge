using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Web.Services;

namespace NphiesBridge.Web.Controllers
{
    public class IcdMappingController : Controller
    {
        private readonly ExcelTemplateService _excelService;
        public IcdMappingController(ExcelTemplateService excelService)
        {
            _excelService = excelService;
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

                // Validate the template
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

                // Store the validated data in TempData for the next step
                TempData["UploadedCodes"] = System.Text.Json.JsonSerializer.Serialize(validationResult.ValidRows);

                return Json(new
                {
                    success = true,
                    message = "Template uploaded and validated successfully!",
                    totalRows = validationResult.TotalRows,
                    validRows = validationResult.ValidRowCount,
                    errorRows = validationResult.ErrorRowCount
                    // Remove redirectUrl - we're staying on the same page
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error processing file: {ex.Message}"
                });
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

        public IActionResult StartMapping()
        {
            // This will be the main mapping interface
            var uploadedCodesJson = TempData["UploadedCodes"] as string;
            if (string.IsNullOrEmpty(uploadedCodesJson))
            {
                TempData["Error"] = "No uploaded codes found. Please upload a template first.";
                return RedirectToAction("SetupCustomMapping");
            }

            var uploadedCodes = System.Text.Json.JsonSerializer.Deserialize<List<ExcelIcdImportDto>>(uploadedCodesJson);

            // TODO: Pass to mapping view
            return View("Mapping", uploadedCodes);
        }
    }
}