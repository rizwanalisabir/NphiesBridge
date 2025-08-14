using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities.ServiceMapping;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServiceMappingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiFuzzyServiceMappingService _aiFuzzyService;

        public ServiceMappingController(
            ApplicationDbContext db,
            IAiFuzzyServiceMappingService aiFuzzyService)
        {
            _db = db;
            _aiFuzzyService = aiFuzzyService;
        }

        [HttpPost("create-session")]
        public ActionResult<ApiResponse<CreateServiceSessionResponseDto>> CreateSession([FromBody] CreateServiceSessionRequestDto request)
        {
            try
            {
                if (request == null ||
                    request.HealthProviderId == Guid.Empty ||
                    request.HealthProviderServiceCodes == null ||
                    request.HealthProviderServiceCodes.Count == 0)
                {
                    return BadRequest(ApiResponse<CreateServiceSessionResponseDto>.ErrorResult("Invalid request"));
                }

                var session = new ServiceMappingSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    FileName = request.FileName,
                    TotalRows = request.HealthProviderServiceCodes.Count,
                    Status = "Created",
                    CreatedAt = DateTime.UtcNow,
                    HealthProviderId = request.HealthProviderId,
                };
                _db.ServiceMappingSessions.Add(session);
                _db.SaveChanges();

                foreach (var row in request.HealthProviderServiceCodes)
                {
                    var svc = new HealthProviderServiceCode
                    {
                        Id = Guid.NewGuid(),
                        HealthProviderServiceId = row.HealthProviderServiceId ?? "",
                        HealthProviderServiceRelation = row.HealthProviderServiceRelation,
                        HealthProviderServiceName = row.HealthProviderServiceName,
                        NphiesServiceCode = row.NphiesServiceCode,
                        ServiceMappingSessionId = session.Id,
                        HealthProviderId = request.HealthProviderId,
                        IsMapped = !string.IsNullOrEmpty(row.NphiesServiceCode)
                    };
                    _db.HealthProviderServiceCodes.Add(svc);
                }
                _db.SaveChanges();

                var resp = new CreateServiceSessionResponseDto
                {
                    SessionId = session.SessionId,
                    TotalRows = session.TotalRows,
                    Status = session.Status
                };

                return Ok(ApiResponse<CreateServiceSessionResponseDto>.SuccessResult(resp));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CreateServiceSessionResponseDto>.ErrorResult($"Failed to create session: {ex.Message}"));
            }
        }

        [HttpGet("session/{sessionId}")]
        public ActionResult<ApiResponse<ServiceMappingPageDto>> GetSession(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                    return BadRequest(ApiResponse<ServiceMappingPageDto>.ErrorResult("SessionId is required"));

                var session = _db.ServiceMappingSessions
                    .Include(x => x.HealthProviderServiceCodes)
                    .FirstOrDefault(x => x.SessionId == sessionId);

                if (session == null)
                    return NotFound(ApiResponse<ServiceMappingPageDto>.ErrorResult("Session not found"));

                var items = session.HealthProviderServiceCodes
                    .OrderBy(x => x.HealthProviderServiceRelation)
                    .Select(x => new HealthProviderServiceCodeDto
                    {
                        Id = x.Id,
                        HealthProviderServiceId = x.HealthProviderServiceId,
                        HealthProviderServiceRelation = x.HealthProviderServiceRelation,
                        HealthProviderServiceName = x.HealthProviderServiceName,
                        NphiesServiceCode = x.NphiesServiceCode,
                        IsMapped = x.IsMapped,
                        MatchConfidence = 0,
                        MappedToCode = x.NphiesServiceCode
                    }).ToList();

                var dto = new ServiceMappingPageDto
                {
                    SessionId = session.SessionId,
                    TotalRows = session.TotalRows,
                    HealthProviderServiceCodes = items
                };

                return Ok(ApiResponse<ServiceMappingPageDto>.SuccessResult(dto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ServiceMappingPageDto>.ErrorResult($"Failed to load session: {ex.Message}"));
            }
        }

        [HttpPost("save-mapping")]
        public ActionResult<ApiResponse<SuccessResponse>> SaveMapping([FromBody] SaveServiceMappingRequest request)
        {
            try
            {
                if (request == null || request.HealthProviderId == Guid.Empty)
                    return BadRequest(ApiResponse<SuccessResponse>.ErrorResult("Invalid request"));

                var svc = _db.HealthProviderServiceCodes.FirstOrDefault(x =>
                    x.HealthProviderServiceId == request.HealthProviderServiceId &&
                    x.HealthProviderServiceRelation == request.HealthProviderServiceRelation &&
                    x.HealthProviderId == request.HealthProviderId);

                if (svc == null)
                    return NotFound(ApiResponse<SuccessResponse>.ErrorResult("Service item not found"));

                svc.NphiesServiceCode = request.NphiesServiceCode;
                svc.IsMapped = true;
                _db.HealthProviderServiceCodes.Update(svc);

                var mapping = new ServiceCodeMapping
                {
                    Id = Guid.NewGuid(),
                    HealthProviderId = request.HealthProviderId,
                    HealthProviderServiceId = request.HealthProviderServiceId,
                    HealthProviderServiceRelation = request.HealthProviderServiceRelation,
                    NphiesServiceCode = request.NphiesServiceCode,
                    MappedByUserId = request.MappedBy,
                    MappedAt = DateTime.UtcNow,
                    IsAiSuggested = request.IsAiSuggested,
                    ConfidenceScore = request.ConfidenceScore.ToString(),
                    ServiceMappingSessionID = svc.ServiceMappingSessionId ?? Guid.Empty
                };
                _db.ServiceCodeMappings.Add(mapping);
                _db.SaveChanges();

                return Ok(ApiResponse<SuccessResponse>.SuccessResult(new SuccessResponse
                {
                    Message = "Mapping saved."
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<SuccessResponse>.ErrorResult($"Failed to save mapping: {ex.Message}"));
            }
        }

        [HttpPost("ai-suggestion")]
        public async Task<ActionResult<ApiResponse<ServiceAiSuggestionResponseDto>>> AiSuggestion([FromBody] ServiceAiSuggestionRequestDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult("Invalid request"));

                var result = await _aiFuzzyService.GetAiSuggestionAsync(request);
                if (result == null)
                    return StatusCode(500, ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult("AI suggestion failed"));

                return Ok(ApiResponse<ServiceAiSuggestionResponseDto>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult($"AI suggestion error: {ex.Message}"));
            }
        }

        [HttpPost("bulk-match")]
        public async Task<ActionResult<ApiResponse<BulkServiceMatchingResponse>>> BulkMatch([FromBody] BulkServiceMatchingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SessionId))
                    return BadRequest(ApiResponse<BulkServiceMatchingResponse>.ErrorResult("SessionId is required"));

                var session = _db.ServiceMappingSessions
                    .Include(x => x.HealthProviderServiceCodes)
                    .FirstOrDefault(x => x.SessionId == request.SessionId);

                if (session == null)
                    return NotFound(ApiResponse<BulkServiceMatchingResponse>.ErrorResult("Session not found"));

                var results = await _aiFuzzyService.BulkSuggestAsync(session.Id, request.BatchSize);

                int processed = 0;
                foreach (var match in results)
                {
                    var svc = session.HealthProviderServiceCodes.FirstOrDefault(x => x.Id == match.HealthProviderServiceCodeId);
                    if (svc != null && !svc.IsMapped)
                    {
                        svc.NphiesServiceCode = match.NphiesServiceCode;
                        svc.IsMapped = true;
                        _db.HealthProviderServiceCodes.Update(svc);

                        var mapping = new ServiceCodeMapping
                        {
                            Id = Guid.NewGuid(),
                            HealthProviderId = session.HealthProviderId,
                            HealthProviderServiceId = svc.HealthProviderServiceId,
                            HealthProviderServiceRelation = svc.HealthProviderServiceRelation,
                            NphiesServiceCode = match.NphiesServiceCode,
                            MappedByUserId = Guid.Empty, // system user
                            MappedAt = DateTime.UtcNow,
                            IsAiSuggested = true,
                            ConfidenceScore = match.ConfidenceScore,
                            ServiceMappingSessionID = session.Id
                        };
                        _db.ServiceCodeMappings.Add(mapping);
                        processed++;
                    }
                }
                _db.SaveChanges();

                var payload = new BulkServiceMatchingResponse
                {
                    Success = true,
                    ProcessedCount = processed,
                    TotalCount = session.HealthProviderServiceCodes.Count,
                    Status = "Bulk match complete",
                    Errors = new List<string>()
                };

                return Ok(ApiResponse<BulkServiceMatchingResponse>.SuccessResult(payload));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BulkServiceMatchingResponse>.ErrorResult($"Bulk match failed: {ex.Message}"));
            }
        }

        [HttpGet("statistics/{sessionId}")]
        public ActionResult<ApiResponse<ServiceMappingStatisticsDto>> GetStatistics(string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                    return BadRequest(ApiResponse<ServiceMappingStatisticsDto>.ErrorResult("SessionId is required"));

                var session = _db.ServiceMappingSessions
                    .Include(x => x.HealthProviderServiceCodes)
                    .FirstOrDefault(x => x.SessionId == sessionId);

                if (session == null)
                    return NotFound(ApiResponse<ServiceMappingStatisticsDto>.ErrorResult("Session not found"));

                int mapped = session.HealthProviderServiceCodes.Count(x => x.IsMapped);
                int unmapped = session.HealthProviderServiceCodes.Count(x => !x.IsMapped);
                int total = session.HealthProviderServiceCodes.Count();

                var stat = new ServiceMappingStatisticsDto
                {
                    SessionId = session.SessionId,
                    TotalServices = total,
                    MappedServices = mapped,
                    UnmappedServices = unmapped,
                    AiSuggestedServices = _db.ServiceCodeMappings.Count(x => x.ServiceMappingSessionID == session.Id && x.IsAiSuggested),
                    ManuallyMappedServices = _db.ServiceCodeMappings.Count(x => x.ServiceMappingSessionID == session.Id && !x.IsAiSuggested),
                    CompletionPercentage = total > 0 ? (decimal)mapped / total * 100 : 0,
                    CreatedAt = session.CreatedAt,
                    LastUpdatedAt = session.CompletedAt
                };

                return Ok(ApiResponse<ServiceMappingStatisticsDto>.SuccessResult(stat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ServiceMappingStatisticsDto>.ErrorResult($"Failed to get statistics: {ex.Message}"));
            }
        }

        // Export stays as a file download (not JSON-enveloped)
        [HttpPost("export")]
        public IActionResult ExportMappings([FromBody] ExportServiceMappingsRequest request)
        {
            var session = _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefault(x => x.SessionId == request.SessionId);

            if (session == null)
                return BadRequest("Session not found");

            

            var fileBytes = GenerateExcelFile(session, request);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceMappings.xlsx");
        }

        private byte[] GenerateExcelFile(ServiceMappingSession session, ExportServiceMappingsRequest request)
        {
            var items = session.HealthProviderServiceCodes
                .Where(x => request.IncludeUnmapped || x.IsMapped)
                .Select(x => new
                {
                    x.HealthProviderServiceId,
                    x.HealthProviderServiceRelation,
                    x.HealthProviderServiceName,
                    x.NphiesServiceCode,
                    x.IsMapped
                })
                .ToList();
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Service Mappings");

                // Set header row (template order and naming)
                worksheet.Cell(1, 1).Value = "ItemId";
                worksheet.Cell(1, 2).Value = "ItemRelation";
                worksheet.Cell(1, 3).Value = "Name";
                worksheet.Cell(1, 4).Value = "NPHIESCode";
                worksheet.Cell(1, 5).Value = "NPHIESDescription";

                int row = 2;
                foreach (var item in items)
                {
                    worksheet.Cell(row, 1).Value = item.HealthProviderServiceId; // ItemId
                    worksheet.Cell(row, 2).Value = item.HealthProviderServiceRelation; // ItemRelation
                    worksheet.Cell(row, 3).Value = item.HealthProviderServiceName; // Name
                    worksheet.Cell(row, 4).Value = item.NphiesServiceCode ?? ""; // NPHIESCode
                    worksheet.Cell(row, 5).Value = item.NphiesServiceCode ?? ""; // NPHIESDescription (if available, else blank)
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}