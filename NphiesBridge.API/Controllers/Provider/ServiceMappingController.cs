using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Core.Entities.ServiceMapping;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
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
        public IActionResult CreateSession([FromBody] CreateServiceSessionRequestDto request)
        {
            if (request.HealthProviderId == Guid.Empty || request.HealthProviderServiceCodes == null || request.HealthProviderServiceCodes.Count == 0)
                return BadRequest("Invalid request");

            var session = new ServiceMappingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                HealthProviderId = request.HealthProviderId,
                FileName = request.FileName,
                TotalRows = request.HealthProviderServiceCodes.Count,
                Status = "Created",
                CreatedAt = DateTime.UtcNow
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
            return Ok(resp);
        }

        [HttpGet("session/{sessionId}")]
        public IActionResult GetSession(string sessionId)
        {
            var session = _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefault(x => x.SessionId == sessionId);

            if (session == null)
                return NotFound("Session not found");

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

            return Ok(dto);
        }

        [HttpPost("save-mapping")]
        public IActionResult SaveMapping([FromBody] SaveServiceMappingRequest request)
        {
            var svc = _db.HealthProviderServiceCodes.FirstOrDefault(x =>
                x.HealthProviderServiceId == request.HealthProviderServiceId &&
                x.HealthProviderServiceRelation == request.HealthProviderServiceRelation &&
                x.HealthProviderId == request.HealthProviderId);

            if (svc == null)
                return NotFound("Service item not found");

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

            return Ok(new { Success = true, Message = "Mapping saved." });
        }

        [HttpPost("ai-suggestion")]
        public async Task<IActionResult> AiSuggestion([FromBody] ServiceAiSuggestionRequestDto request)
        {
            var result = await _aiFuzzyService.GetAiSuggestionAsync(request);
            return Ok(result);
        }

        [HttpPost("bulk-match")]
        public async Task<IActionResult> BulkMatch([FromBody] BulkServiceMatchingRequest request)
        {
            var session = _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefault(x => x.SessionId == request.SessionId);

            if (session == null)
                return NotFound("Session not found");

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

            return Ok(new BulkServiceMatchingResponse
            {
                Success = true,
                ProcessedCount = processed,
                TotalCount = session.HealthProviderServiceCodes.Count,
                Status = "Bulk match complete",
                Errors = new List<string>()
            });
        }

        [HttpGet("statistics/{sessionId}")]
        public IActionResult GetStatistics(string sessionId)
        {
            var session = _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefault(x => x.SessionId == sessionId);

            if (session == null)
                return NotFound("Session not found");

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
            return Ok(stat);
        }

        [HttpGet("export")]
        public IActionResult ExportMappings([FromQuery] ExportServiceMappingsRequest request)
        {
            var session = _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefault(x => x.SessionId == request.SessionId);

            if (session == null)
                return BadRequest("Session not found");

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

            // TODO: Generate Excel/CSV file from items
            var fileBytes = GenerateExcelFile(items);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceMappings.xlsx");
        }

        private byte[] GenerateExcelFile(object items)
        {
            // For brevity, return empty byte array.
            // Replace with actual Excel generation logic using EPPlus or ClosedXML as needed.
            return new byte[0];
        }
    }
}