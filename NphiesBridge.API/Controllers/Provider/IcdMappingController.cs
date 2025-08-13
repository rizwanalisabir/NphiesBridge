using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities.IcdMapping;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Infrastructure.Repositories;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.DTOs.NphiesBridge.Shared.DTOs;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Claims;

namespace NphiesBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class IcdMappingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        //private readonly IAiFuzzyMatchingService _aiMatchingService;
        private readonly IAiFuzzyMatchingService _aiFuzzyMatchingService;
        private readonly ILogger<IcdMappingController> _logger;

        public IcdMappingController(
            ApplicationDbContext context,
            IAiFuzzyMatchingService aiFuzzyMatchingService,
            ILogger<IcdMappingController> logger)
        {
            _context = context;
            _aiFuzzyMatchingService = aiFuzzyMatchingService;
            _logger = logger;
        }

        /// <summary>
        /// Get AI suggestion for a hospital code
        /// </summary>
        // File: NphiesBridge.API/Controllers/IcdMappingController.cs
        [HttpPost("ai-suggestion")]
        public async Task<ActionResult<ApiResponse<AiSuggestionResponseDto>>> GetAiSuggestion([FromBody] AiSuggestionRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AiSuggestionResponseDto>.ErrorResult("Invalid request"));
                }

                var result = await _aiFuzzyMatchingService.GetAiSuggestionAsync(request);

                if (result.IsSuccess)
                {
                    return Ok(ApiResponse<AiSuggestionResponseDto>.SuccessResult(result.Data));
                }

                return StatusCode(500, ApiResponse<AiSuggestionResponseDto>.ErrorResult(result.ErrorMessage ?? "AI matching failed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAiSuggestion API endpoint");
                return StatusCode(500, ApiResponse<AiSuggestionResponseDto>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Get mapping statistics for a session
        /// </summary>
        [HttpGet("statistics/{sessionId}")]
        public async Task<ActionResult<ApiResponse<MappingStatisticsDto>>> GetMappingStatistics(Guid sessionId)
        {
            try
            {
                var session = await _context.MappingSessions
                    .Include(s => s.HospitalCodes)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return NotFound(ApiResponse<MappingStatisticsDto>.ErrorResult("Session not found"));
                }

                var totalCodes = session.HospitalCodes.Count;
                var mappedCodes = session.HospitalCodes.Count(h => h.IsMapped);
                var unmappedCodes = totalCodes - mappedCodes;

                var statistics = new MappingStatisticsDto
                {
                    SessionId = sessionId,
                    TotalCodes = totalCodes,
                    MappedCodes = mappedCodes,
                    UnmappedCodes = unmappedCodes,
                    CompletionPercentage = totalCodes > 0 ? (double)mappedCodes / totalCodes * 100 : 0,
                    Status = session.Status,
                    LastUpdated = session.UpdatedAt
                };

                return Ok(ApiResponse<MappingStatisticsDto>.SuccessResult(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapping statistics for session: {SessionId}", sessionId);
                return StatusCode(500, ApiResponse<MappingStatisticsDto>.ErrorResult("Failed to retrieve statistics"));
            }
        }

        /// <summary>
        /// Export mappings to Excel
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportMappings([FromBody] ExportMappingsRequestDto request)
        {
            try
            {
                _logger.LogInformation("Exporting mappings for session: {SessionId}", request.SessionId);

                var session = await _context.MappingSessions
                    .Include(s => s.HealthProvider)
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session == null)
                {
                    return BadRequest(new { message = "Session not found" });
                }

                // Get mappings with related data
                var mappingsQuery = from m in _context.IcdCodeMappings
                                    join u in _context.Users
                                        on m.MappedByUserId equals u.Id
                                    join ms in _context.MappingSessions
                                        on m.MappingSessionID equals ms.SessionId
                                    join hc in _context.HospitalIcdCodes
                                        on m.HealthProviderIcdCode equals hc.HospitalCode
                                    where ms.SessionId == session.SessionId
                                    select new
                                    {
                                        HospitalCode = hc.HospitalCode,
                                        DiagnosisName = hc.DiagnosisName,
                                        DiagnosisDescription = hc.DiagnosisDescription,
                                        SuggestedIcd10Am = hc.SuggestedIcd10Am,
                                        NphiesIcdCode = m.NphiesIcdCode,
                                        MappedBy = u.FirstName + " " + u.LastName,
                                        MappedAt = m.MappedAt,
                                        IsAiSuggested = m.IsAiSuggested,
                                        ConfidenceScore = m.ConfidenceScore
                                    };
                var mappings = await mappingsQuery
                    .OrderBy(m => m.HospitalCode)
                    .ToListAsync();

                if (!mappings.Any())
                {
                    return BadRequest(new { message = "No mappings found to export" });
                }

                // Get NPHIES descriptions for the mapped codes
                var nphiesCodes = await _context.NphiesIcdCodes
                    .Where(n => mappings.Select(m => m.NphiesIcdCode).Contains(n.Code))
                    .ToDictionaryAsync(n => n.Code, n => n.Description);

                // Create Excel file
                var excelData = GenerateExcelFile(mappings, nphiesCodes, session);

                var fileName = $"ICD_Mappings_{session.HealthProvider.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

                // Update session as exported
                session.Status = "Exported";
                session.UpdatedAt = DateTime.UtcNow;
                _context.MappingSessions.Update(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Mappings exported successfully for session: {SessionId}", request.SessionId);

                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting mappings for session: {SessionId}", request.SessionId);
                return StatusCode(500, new { message = "Failed to export mappings. Please try again." });
            }
        }

        /// <summary>
        /// Process bulk AI matching for a session
        /// </summary>
        [HttpPost("bulk-match")]
        public async Task<ActionResult<ApiResponse<BulkMatchingResponseDto>>> ProcessBulkMatching([FromBody] BulkMatchingRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing bulk matching for session: {SessionId}", request.SessionId);

                var result = await _aiFuzzyMatchingService.ProcessBulkMatchingAsync(request);

                if (!result.IsSuccess)
                {
                    return BadRequest(ApiResponse<BulkMatchingResponseDto>.ErrorResult(result.ErrorMessage ?? "Bulk matching failed"));
                }

                return Ok(ApiResponse<BulkMatchingResponseDto>.SuccessResult(result.Data!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk matching for session: {SessionId}", request.SessionId);
                return StatusCode(500, ApiResponse<BulkMatchingResponseDto>.ErrorResult("Failed to process bulk matching"));
            }
        }

        // ============================================
        // PRIVATE HELPER METHODS
        // ============================================

        private async Task UpdateSessionStatistics(Guid sessionId)
        {
            try
            {
                var session = await _context.MappingSessions
                    .Include(s => s.HospitalCodes)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session != null)
                {
                    var mappedCount = session.HospitalCodes.Count(h => h.IsMapped);

                    session.ProcessedRows = mappedCount;
                    session.UpdatedAt = DateTime.UtcNow;

                    if (mappedCount == session.TotalRows)
                    {
                        session.Status = "Completed";
                        session.CompletedAt = DateTime.UtcNow;
                    }

                    _context.MappingSessions.Update(session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session statistics for session ID: {SessionId}", sessionId);
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }

            // Fallback - you might want to handle this differently
            _logger.LogWarning("Could not get current user ID from claims");
            return Guid.Empty;
        }

        private byte[] GenerateExcelFile(dynamic mappings, Dictionary<string, string> nphiesCodes, ServiceMappingSession session)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.AddWorksheet("ICD Mappings");

                // Simple headers
                worksheet.Cell("A1").Value = "ICD-10-AM Code";
                worksheet.Cell("B1").Value = "Hospital ICD Code";
                worksheet.Cell("C1").Value = "Diagnosis Name";
                worksheet.Cell("D1").Value = "Diagnosis Description";

                // Simple data
                int row = 2;
                foreach (var mapping in mappings)
                {
                    worksheet.Cell(row, 1).Value = mapping.NphiesIcdCode?.ToString() ?? "";
                    worksheet.Cell(row, 2).Value = mapping.HospitalCode?.ToString() ?? "";
                    worksheet.Cell(row, 3).Value = mapping.DiagnosisName?.ToString() ?? "";
                    worksheet.Cell(row, 4).Value = mapping.DiagnosisDescription?.ToString() ?? "";
                    row++;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file");
                throw;
            }
        }

        /// <summary>
        /// Create mapping session and save uploaded hospital codes
        /// </summary>
        [HttpPost("create-session")]
        public async Task<ActionResult<ApiResponse<CreateSessionResponseDto>>> CreateMappingSession([FromBody] CreateSessionRequestDto request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(ApiResponse<CreateSessionResponseDto>.ErrorResult("Request is null"));
                }

                if (request.SessionId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<CreateSessionResponseDto>.ErrorResult("SessionId is required"));
                }

                if (request.HealthProviderId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<CreateSessionResponseDto>.ErrorResult("HealthProviderId is required"));
                }

                _logger.LogInformation("Creating mapping session for {Count} hospital codes", request.HospitalCodes?.Count ?? 0);

                // Check if session already exists
                var existingSession = await _context.MappingSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (existingSession != null)
                {
                    return BadRequest(ApiResponse<CreateSessionResponseDto>.ErrorResult("Session already exists"));
                }

                // Verify HealthProvider exists
                var healthProvider = await _context.HealthProviders
                    .FirstOrDefaultAsync(hp => hp.Id == request.HealthProviderId);

                if (healthProvider == null)
                {
                    return BadRequest(ApiResponse<CreateSessionResponseDto>.ErrorResult("HealthProvider not found"));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create mapping session
                    var session = new ServiceMappingSession
                    {
                        Id = Guid.NewGuid(),
                        SessionId = request.SessionId,
                        HealthProviderId = request.HealthProviderId,
                        OriginalFileName = request.FileName ?? "uploaded_file.xlsx",
                        TotalRows = request.HospitalCodes?.Count ?? 0,
                        ProcessedRows = 0,
                        Status = "Processing",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false // If BaseEntity has this property
                    };

                    _context.MappingSessions.Add(session);

                    // Save session first to get the ID
                    await _context.SaveChangesAsync();

                    // Save hospital codes to database
                    if (request.HospitalCodes?.Any() == true)
                    {
                        var hospitalCodes = new List<HospitalServiceCode>();

                        foreach (var code in request.HospitalCodes)
                        {
                            var hospitalCode = new HospitalServiceCode
                            {
                                Id = Guid.NewGuid(),
                                HospitalCode = code.HospitalCode ?? "",
                                DiagnosisName = code.DiagnosisName ?? "",
                                DiagnosisDescription = code.DiagnosisDescription,
                                SuggestedIcd10Am = code.SuggestedIcd10Am,
                                IsMapped = false,
                                MappingSessionId = session.Id, // Use the saved session ID
                                HealthProviderId = request.HealthProviderId,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsDeleted = false // If BaseEntity has this property
                            };

                            hospitalCodes.Add(hospitalCode);
                        }

                        _context.HospitalIcdCodes.AddRange(hospitalCodes);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    var response = new CreateSessionResponseDto
                    {
                        SessionId = session.SessionId,
                        TotalRows = session.TotalRows,
                        Message = "Session created successfully"
                    };

                    _logger.LogInformation("Mapping session created successfully: {SessionId} with {Count} codes",
                        session.SessionId, request.HospitalCodes?.Count ?? 0);

                    return Ok(ApiResponse<CreateSessionResponseDto>.SuccessResult(response));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw; // Re-throw to be caught by outer catch
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mapping session for SessionId: {SessionId}", request?.SessionId);
                return StatusCode(500, ApiResponse<CreateSessionResponseDto>.ErrorResult($"Failed to create mapping session: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get hospital codes for a mapping session (updated method)
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<ApiResponse<IcdMappingPageDto>>> GetMappingSession(Guid sessionId)
        {
            try
            {
                if (sessionId == Guid.Empty)
                {
                    return BadRequest(ApiResponse<IcdMappingPageDto>.ErrorResult("Session ID is required"));
                }

                var session = await _context.MappingSessions
                    .Include(s => s.HealthProvider)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return NotFound(ApiResponse<IcdMappingPageDto>.ErrorResult("Mapping session not found"));
                }

                // Get hospital codes for this session
                var hospitalCodes = await _context.HospitalIcdCodes
                    .Where(h => h.MappingSessionId == session.Id)
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new HospitalCodeDto
                    {
                        Id = h.Id,
                        HospitalCode = h.HospitalCode,
                        DiagnosisName = h.DiagnosisName,
                        DiagnosisDescription = h.DiagnosisDescription,
                        SuggestedIcd10Am = h.SuggestedIcd10Am,
                        IsMapped = h.IsMapped
                    })
                    .ToListAsync();

                var result = new IcdMappingPageDto
                {
                    SessionId = sessionId,
                    TotalRows = hospitalCodes.Count,
                    HospitalCodes = hospitalCodes
                };

                _logger.LogInformation("Retrieved mapping session: {SessionId} with {Count} codes",
                    sessionId, hospitalCodes.Count);

                return Ok(ApiResponse<IcdMappingPageDto>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mapping session: {SessionId}", sessionId);
                return StatusCode(500, ApiResponse<IcdMappingPageDto>.ErrorResult("Failed to retrieve mapping session"));
            }
        }

        [HttpPost("save-mapping")]
        public async Task<IActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            try
            {
                _logger.LogInformation("Saving mapping for Health Provider: {HealthProviderId}", request.HealthProviderId);

                // Get current user ID
                //var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (!Guid.TryParse(userIdClaim, out var userId))
                //{
                //    return BadRequest(new { success = false, message = "Invalid user ID" });
                //}

                // Validate hospital code exists
                var hospitalCode = await _context.HospitalIcdCodes
                    .FirstOrDefaultAsync(h => h.HospitalCode == request.HospitalIcdCode);

                if (hospitalCode == null)
                {
                    return NotFound(new { success = false, message = "Hospital code not found" });
                }

                // Check if mapping already exists
                var existingMapping = await _context.IcdCodeMappings
                    .FirstOrDefaultAsync(m => m.HealthProviderIcdCode == request.HospitalIcdCode && !m.IsDeleted);

                if (existingMapping != null)
                {
                    // Update existing mapping
                    existingMapping.NphiesIcdCode = request.NphiesIcdCode;
                    existingMapping.MappedByUserId = request.MappedBy;
                    existingMapping.MappedAt = DateTime.UtcNow;
                    existingMapping.IsAiSuggested = request.IsAiSuggested;
                    existingMapping.ConfidenceScore = request.ConfidenceScore;
                    existingMapping.UpdatedAt = DateTime.UtcNow;
                    existingMapping.HealthProviderIcdCode = request.HospitalIcdCode;

                    _context.IcdCodeMappings.Update(existingMapping);
                    _logger.LogInformation("Updated existing mapping with ID: {MappingId}", existingMapping.Id);
                }
                else
                {
                    // Create new mapping
                    var newMapping = new ServiceCodeMapping
                    {
                        Id = Guid.NewGuid(),
                        HealthProviderId = request.HealthProviderId,
                        NphiesIcdCode = request.NphiesIcdCode,
                        MappedByUserId = request.MappedBy,
                        MappedAt = DateTime.UtcNow,
                        IsAiSuggested = request.IsAiSuggested,
                        ConfidenceScore = request.ConfidenceScore,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        HealthProviderIcdCode = request.HospitalIcdCode
                    };

                    await _context.IcdCodeMappings.AddAsync(newMapping);
                    _logger.LogInformation("Created new mapping with ID: {MappingId}", newMapping.Id);
                }

                // Update hospital code mapping status
                hospitalCode.IsMapped = true;
                hospitalCode.UpdatedAt = DateTime.UtcNow;
                _context.HospitalIcdCodes.Update(hospitalCode);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = existingMapping != null ? "Mapping updated successfully" : "Mapping saved successfully",
                    data = new { hospitalCodeId = request.HealthProviderId, isMapped = true }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving mapping for Health Provider: {HealthProviderId}", request.HealthProviderId);
                return StatusCode(500, new { success = false, message = "An error occurred while saving the mapping" });
            }
        }

        [HttpPost("save-bulk-mappings")]
        public async Task<IActionResult> SaveBulkMappings([FromBody] SaveBulkMappingsRequest request)
        {
            try
            {
                _logger.LogInformation("Saving bulk mappings for {Count} items", request.Mappings.Count);

                // Get current user ID
                //var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (!Guid.TryParse(userIdClaim, out var userId))
                //{
                //    return BadRequest(new { success = false, message = "Invalid user ID" });
                //}

                var savedCount = 0;
                var updatedCount = 0;
                var failedMappings = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var mappingRequest in request.Mappings)
                    {
                        try
                        {
                            // Validate hospital code exists
                            var hospitalCode = await _context.HospitalIcdCodes
                    .FirstOrDefaultAsync(h => h.HospitalCode == mappingRequest.HospitalIcdCode);

                            if (hospitalCode == null)
                            {
                                failedMappings.Add($"Hospital code {mappingRequest.HealthProviderId} not found");
                                continue;
                            }

                            // Check if mapping already exists
                            var existingMapping = await _context.IcdCodeMappings
                    .FirstOrDefaultAsync(m => m.HealthProviderIcdCode == mappingRequest.HospitalIcdCode && !m.IsDeleted);

                            if (existingMapping != null)
                            {
                                // Update existing mapping
                                existingMapping.NphiesIcdCode = mappingRequest.NphiesIcdCode;
                                existingMapping.MappedByUserId = mappingRequest.MappedBy;
                                existingMapping.MappedAt = DateTime.UtcNow;
                                existingMapping.IsAiSuggested = mappingRequest.IsAiSuggested;
                                existingMapping.ConfidenceScore = mappingRequest.ConfidenceScore;
                                existingMapping.UpdatedAt = DateTime.UtcNow;
                                existingMapping.HealthProviderIcdCode = mappingRequest.HospitalIcdCode;
                                existingMapping.MappingSessionID = mappingRequest.MappingSessionId;

                                _context.IcdCodeMappings.Update(existingMapping);
                                updatedCount++;
                            }
                            else
                            {
                                // Create new mapping
                                var newMapping = new ServiceCodeMapping
                                {
                                    Id = Guid.NewGuid(),
                                    HealthProviderId = mappingRequest.HealthProviderId,
                                    NphiesIcdCode = mappingRequest.NphiesIcdCode,
                                    MappedByUserId = mappingRequest.MappedBy,
                                    MappedAt = DateTime.UtcNow,
                                    IsAiSuggested = mappingRequest.IsAiSuggested,
                                    ConfidenceScore = mappingRequest.ConfidenceScore,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    IsDeleted = false,
                                    HealthProviderIcdCode = mappingRequest.HospitalIcdCode,
                                    MappingSessionID = mappingRequest.MappingSessionId
                                };

                                await _context.IcdCodeMappings.AddAsync(newMapping);
                                savedCount++;
                            }

                            // Update hospital code mapping status
                            hospitalCode.IsMapped = true;
                            hospitalCode.UpdatedAt = DateTime.UtcNow;
                            _context.HospitalIcdCodes.Update(hospitalCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing mapping for HealthProviderId: {HealthProviderId}", mappingRequest.HealthProviderId);
                            failedMappings.Add($"Failed to process mapping for {mappingRequest.HealthProviderId}");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var totalProcessed = savedCount + updatedCount;
                    var message = $"Successfully processed {totalProcessed} mappings ({savedCount} new, {updatedCount} updated)";

                    if (failedMappings.Any())
                    {
                        message += $". {failedMappings.Count} mappings failed.";
                    }

                    return Ok(new
                    {
                        success = true,
                        message = message,
                        data = new
                        {
                            totalProcessed = totalProcessed,
                            savedCount = savedCount,
                            updatedCount = updatedCount,
                            failedCount = failedMappings.Count,
                            failedMappings = failedMappings
                        }
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bulk mappings");
                return StatusCode(500, new { success = false, message = "An error occurred while saving bulk mappings" });
            }
        }

        [HttpGet("check-mappings/{sessionId}")]
        public async Task<IActionResult> CheckExistingMappings(Guid sessionId)
        {
            try
            {
                var hospitalCodes = await _context.HospitalIcdCodes
                    .Where(h => h.MappingSessionId == sessionId)
                    .Select(h => h.Id)
                    .ToListAsync();

                var existingMappings = await _context.IcdCodeMappings
                    .Where(m => hospitalCodes.Contains(m.HealthProviderId) && !m.IsDeleted)
                    .Select(m => new { m.HealthProviderId, m.NphiesIcdCode, m.ConfidenceScore })
                    .ToListAsync();

                return Ok(new { success = true, data = existingMappings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing mappings for session: {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "An error occurred while checking existing mappings" });
            }
        }
    }

    // Additional DTO for statistics
    public class MappingStatisticsDto
    {
        public Guid SessionId { get; set; }
        public int TotalCodes { get; set; }
        public int MappedCodes { get; set; }
        public int UnmappedCodes { get; set; }
        public double CompletionPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
    }

    // API Response wrapper
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}