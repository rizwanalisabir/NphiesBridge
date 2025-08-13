using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NphiesBridge.Core.Entities.ServiceCodesMapping;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System.Collections.Concurrent;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServiceCodesMappingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiServiceMatchingService _aiServiceMatching;
        private readonly ILogger<ServiceCodesMappingController> _logger;
        private readonly int _highConfidence;

        public ServiceCodesMappingController(
            ApplicationDbContext db,
            IAiServiceMatchingService aiServiceMatching,
            IConfiguration config,
            ILogger<ServiceCodesMappingController> logger)
        {
            _db = db;
            _aiServiceMatching = aiServiceMatching;
            _logger = logger;

            var thresholdStr = config["ServiceCodeMapping:HighConfidenceThreshold"];
            _highConfidence = int.TryParse(thresholdStr, out var parsed) ? parsed : 90;
        }

        // Create session via Excel upload (ItemRelation is mandatory, ItemId is optional, Name should be present)
        [HttpPost("session/upload-excel")]
        public async Task<ActionResult<ApiResponse<CreateServiceMappingSessionResponseDto>>> CreateSessionViaExcel(
            [FromQuery] Guid healthProviderId,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult("No file uploaded"));

            try
            {
                var items = new List<ExcelServiceImportDto>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using var workbook = new XLWorkbook(stream);
                    var sheet = workbook.Worksheets.FirstOrDefault();
                    if (sheet == null)
                        return BadRequest(ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult("Excel contains no worksheets"));

                    var headerRow = sheet.FirstRowUsed();
                    var firstDataRow = headerRow.RowBelow();

                    // Map headers
                    var headers = headerRow.Cells().ToDictionary(
                        c => c.GetString().Trim(),
                        c => c.Address.ColumnNumber,
                        StringComparer.OrdinalIgnoreCase);

                    string[] requiredHeaders = { "ItemRelation"};
                    foreach (var h in requiredHeaders)
                    {
                        if (!headers.ContainsKey(h))
                            return BadRequest(ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult($"Missing required column: {h}"));
                    }

                    // Optional headers
                    int colItemId = headers.TryGetValue("ItemId", out var cItemId) ? cItemId : -1;
                    int colItemRelation = headers["ItemRelation"];
                    int colName = headers["Name"];
                    int colNphiesCode = headers.TryGetValue("NPHIESCode", out var cNphiesCode) ? cNphiesCode : -1;
                    int colNphiesDesc = headers.TryGetValue("NPHIESDescription", out var cNphiesDesc) ? cNphiesDesc : -1;

                    foreach (var row in firstDataRow.RowUsed().Worksheet.RowsUsed().Skip(firstDataRow.RowNumber() - 1))
                    {
                        if (row.IsEmpty()) continue;

                        var dto = new ExcelServiceImportDto
                        {
                            ItemId = colItemId > 0 ? row.Cell(colItemId).GetString().Trim() : null,
                            ItemRelation = row.Cell(colItemRelation).GetString().Trim(),
                            Name = row.Cell(colName).GetString().Trim(),
                            NphiesCode = colNphiesCode > 0 ? row.Cell(colNphiesCode).GetString().Trim() : null,
                            NphiesDescription = colNphiesDesc > 0 ? row.Cell(colNphiesDesc).GetString().Trim() : null
                        };

                        // ItemRelation must exist; Name should exist but can be empty string; ItemId optional
                        if (string.IsNullOrWhiteSpace(dto.ItemRelation))
                            continue;

                        items.Add(dto);
                    }
                }

                var response = await CreateSessionInternal(healthProviderId, items, file.FileName);
                return Ok(ApiResponse<CreateServiceMappingSessionResponseDto>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ServiceCodes mapping session via Excel");
                return StatusCode(500, ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult("Internal server error"));
            }
        }

        // Create session via JSON (UI can parse Excel and call this)
        [HttpPost("session/create")]
        public async Task<ActionResult<ApiResponse<CreateServiceMappingSessionResponseDto>>> CreateSessionViaJson(
            [FromQuery] Guid healthProviderId,
            [FromBody] CreateServiceMappingSessionRequestDto request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
                return BadRequest(ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult("No items provided"));

            try
            {
                // Enforce ItemRelation presence, allow Name empty, ItemId optional
                var filtered = request.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemRelation))
                    .ToList();

                var response = await CreateSessionInternal(healthProviderId, filtered, request.OriginalFileName);
                return Ok(ApiResponse<CreateServiceMappingSessionResponseDto>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ServiceCodes mapping session via JSON");
                return StatusCode(500, ApiResponse<CreateServiceMappingSessionResponseDto>.ErrorResult("Internal server error"));
            }
        }

        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<ApiResponse<ServiceMappingPageDto>>> GetSession(
            string sessionId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var session = await _db.ServiceCodesMappingSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return NotFound(ApiResponse<ServiceMappingPageDto>.ErrorResult("Session not found"));

                if (pageSize <= 0 || pageSize > 100) pageSize = 100;
                if (page <= 0) page = 1;

                var query = _db.ProviderServiceItems
                    .AsNoTracking()
                    .Where(p => p.ServiceCodesMappingSessionId == session.Id)
                    .OrderBy(p => p.Id);

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProviderServiceItemDto
                    {
                        Id = p.Id,
                        ItemId = p.ItemId,
                        ItemRelation = p.ItemRelation,
                        Name = p.Name,
                        NphiesCode = p.NphiesCode,
                        NphiesDescription = p.NphiesDescription,
                        IsMapped = p.IsMapped,
                        SuggestedNphiesCode = p.SuggestedNphiesCode,
                        ConfidenceScore = p.ConfidenceScore,
                        MatchReason = p.MatchReason
                    })
                    .ToListAsync();

                var dto = new ServiceMappingPageDto
                {
                    SessionId = session.SessionId,
                    TotalRows = session.TotalRows,
                    ProviderItems = items
                };

                return Ok(ApiResponse<ServiceMappingPageDto>.SuccessResult(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ServiceCodes mapping session: {SessionId}", sessionId);
                return StatusCode(500, ApiResponse<ServiceMappingPageDto>.ErrorResult("Internal server error"));
            }
        }

        [HttpPost("ai-suggestion")]
        public async Task<ActionResult<ApiResponse<ServiceAiSuggestionResponseDto>>> GetAiSuggestion([FromBody] ServiceAiSuggestionRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult("Name is required"));

            var result = await _aiServiceMatching.GetAiSuggestionAsync(request);
            if (!result.IsSuccess || result.Data == null)
                return Ok(ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult(result.ErrorMessage ?? "No suggestion"));

            return Ok(ApiResponse<ServiceAiSuggestionResponseDto>.SuccessResult(result.Data));
        }

        [HttpPost("ai-suggestions/bulk")]
        public async Task<ActionResult<ApiResponse<BulkServiceMatchingResponseDto>>> GetAiSuggestionsBulk([FromBody] BulkServiceMatchingRequestDto request)
        {
            var result = await _aiServiceMatching.ProcessBulkMatchingAsync(request);
            if (!result.IsSuccess || result.Data == null)
                return Ok(ApiResponse<BulkServiceMatchingResponseDto>.ErrorResult(result.ErrorMessage ?? "Bulk suggestion failed"));

            return Ok(ApiResponse<BulkServiceMatchingResponseDto>.SuccessResult(result.Data));
        }

        [HttpPost("mappings/save")]
        public async Task<ActionResult<ApiResponse>> SaveMapping([FromBody] CreateServiceCodeMappingDto request)
        {
            try
            {
                var providerItem = await _db.ProviderServiceItems
                    .FirstOrDefaultAsync(p => p.Id == request.ProviderServiceItemId);

                if (providerItem == null)
                    return NotFound(ApiResponse.ErrorResult("Provider service item not found"));

                var healthProviderId = providerItem.HealthProviderId;
                var itemRelation = providerItem.ItemRelation;
                var itemId = providerItem.ItemId;
                var providerName = providerItem.Name;

                // Upsert mapping by (HealthProviderId, ProviderItemRelation)
                var existing = await _db.ServiceCodeMappings
                    .FirstOrDefaultAsync(m => m.HealthProviderId == healthProviderId &&
                                              m.ProviderItemRelation == itemRelation);

                if (existing == null)
                {
                    var mapping = new ServiceCodeMapping
                    {
                        HealthProviderId = healthProviderId,
                        ProviderItemRelation = itemRelation,
                        ProviderItemId = itemId,
                        ProviderItemName = providerName,
                        NphiesServiceCodeValue = request.NphiesServiceCode,
                        IsAiSuggested = request.IsAiSuggested,
                        ConfidenceScore = request.ConfidenceScore
                    };
                    _db.ServiceCodeMappings.Add(mapping);
                }
                else
                {
                    existing.NphiesServiceCodeValue = request.NphiesServiceCode;
                    existing.IsAiSuggested = request.IsAiSuggested;
                    existing.ConfidenceScore = request.ConfidenceScore;
                    existing.MappedAt = DateTime.UtcNow;
                    existing.ProviderItemId = itemId;
                    existing.ProviderItemName = providerName;
                    _db.ServiceCodeMappings.Update(existing);
                }

                // Mark provider item as mapped
                providerItem.IsMapped = true;
                providerItem.NphiesCode = request.NphiesServiceCode;
                _db.ProviderServiceItems.Update(providerItem);

                await _db.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResult("Mapping saved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service code mapping");
                return StatusCode(500, ApiResponse.ErrorResult("Internal server error"));
            }
        }

        [HttpPost("mappings/save-bulk")]
        public async Task<ActionResult<object>> SaveMappingsBulk([FromBody] List<CreateServiceCodeMappingDto> requests)
        {
            if (requests == null || requests.Count == 0)
                return BadRequest(new { success = false, message = "No mappings provided" });

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                int saved = 0, updated = 0;
                var errors = new List<string>();

                foreach (var req in requests)
                {
                    try
                    {
                        var providerItem = await _db.ProviderServiceItems
                            .FirstOrDefaultAsync(p => p.Id == req.ProviderServiceItemId);

                        if (providerItem == null)
                        {
                            errors.Add($"ProviderServiceItem not found: {req.ProviderServiceItemId}");
                            continue;
                        }

                        var existing = await _db.ServiceCodeMappings
                            .FirstOrDefaultAsync(m => m.HealthProviderId == providerItem.HealthProviderId &&
                                                      m.ProviderItemRelation == providerItem.ItemRelation);

                        if (existing == null)
                        {
                            _db.ServiceCodeMappings.Add(new ServiceCodeMapping
                            {
                                HealthProviderId = providerItem.HealthProviderId,
                                ProviderItemRelation = providerItem.ItemRelation,
                                ProviderItemId = providerItem.ItemId,
                                ProviderItemName = providerItem.Name,
                                NphiesServiceCodeValue = req.NphiesServiceCode,
                                IsAiSuggested = req.IsAiSuggested,
                                ConfidenceScore = req.ConfidenceScore
                            });
                            saved++;
                        }
                        else
                        {
                            existing.NphiesServiceCodeValue = req.NphiesServiceCode;
                            existing.IsAiSuggested = req.IsAiSuggested;
                            existing.ConfidenceScore = req.ConfidenceScore;
                            existing.MappedAt = DateTime.UtcNow;
                            existing.ProviderItemId = providerItem.ItemId;
                            existing.ProviderItemName = providerItem.Name;
                            _db.ServiceCodeMappings.Update(existing);
                            updated++;
                        }

                        providerItem.IsMapped = true;
                        providerItem.NphiesCode = req.NphiesServiceCode;
                        _db.ProviderServiceItems.Update(providerItem);
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Failed to process mapping for provider item: {ProviderItemId}", req.ProviderServiceItemId);
                        errors.Add($"Failed mapping for {req.ProviderServiceItemId}");
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Processed {saved + updated} mappings",
                    data = new { saved, updated, failedCount = errors.Count, failed = errors }
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error in bulk save mappings");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("statistics/{sessionId}")]
        public async Task<ActionResult<ApiResponse<ServiceMappingStatisticsDto>>> GetStatistics(string sessionId)
        {
            try
            {
                var session = await _db.ServiceCodesMappingSessions
                    .Include(s => s.ProviderItems)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return NotFound(ApiResponse<ServiceMappingStatisticsDto>.ErrorResult("Session not found"));

                var total = session.ProviderItems.Count;
                var mapped = session.ProviderItems.Count(p => p.IsMapped);
                var stats = new ServiceMappingStatisticsDto
                {
                    SessionId = session.SessionId,
                    TotalCodes = total,
                    MappedCodes = mapped,
                    UnmappedCodes = total - mapped,
                    CompletionPercentage = total > 0 ? (double)mapped / total * 100.0 : 0.0,
                    Status = session.Status,
                    LastUpdated = session.CompletedAt ?? DateTime.UtcNow
                };

                return Ok(ApiResponse<ServiceMappingStatisticsDto>.SuccessResult(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for session {SessionId}", sessionId);
                return StatusCode(500, ApiResponse<ServiceMappingStatisticsDto>.ErrorResult("Internal server error"));
            }
        }

        [HttpPost("approve-all-high/{sessionId}")]
        public async Task<ActionResult<ApiResponse>> ApproveAllHigh(string sessionId, [FromQuery] int max = 100)
        {
            try
            {
                var session = await _db.ServiceCodesMappingSessions
                    .Include(s => s.ProviderItems)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return NotFound(ApiResponse.ErrorResult("Session not found"));

                var items = session.ProviderItems
                    .Where(p => !p.IsMapped && !string.IsNullOrWhiteSpace(p.SuggestedNphiesCode) && p.ConfidenceScore >= _highConfidence)
                    .OrderByDescending(p => p.ConfidenceScore)
                    .Take(Math.Max(1, Math.Min(100, max))) // restrict to 100 max at a time
                    .ToList();

                int saved = 0;

                foreach (var p in items)
                {
                    // Upsert mapping
                    var existing = await _db.ServiceCodeMappings
                        .FirstOrDefaultAsync(m => m.HealthProviderId == p.HealthProviderId &&
                                                  m.ProviderItemRelation == p.ItemRelation);

                    if (existing == null)
                    {
                        _db.ServiceCodeMappings.Add(new ServiceCodeMapping
                        {
                            HealthProviderId = p.HealthProviderId,
                            ProviderItemRelation = p.ItemRelation,
                            ProviderItemId = p.ItemId,
                            ProviderItemName = p.Name,
                            NphiesServiceCodeValue = p.SuggestedNphiesCode!,
                            IsAiSuggested = true,
                            ConfidenceScore = p.ConfidenceScore.ToString()
                        });
                    }
                    else
                    {
                        existing.NphiesServiceCodeValue = p.SuggestedNphiesCode!;
                        existing.IsAiSuggested = true;
                        existing.ConfidenceScore = p.ConfidenceScore.ToString();
                        existing.MappedAt = DateTime.UtcNow;
                        existing.ProviderItemId = p.ItemId;
                        existing.ProviderItemName = p.Name;
                        _db.ServiceCodeMappings.Update(existing);
                    }

                    p.IsMapped = true;
                    p.NphiesCode = p.SuggestedNphiesCode;
                    _db.ProviderServiceItems.Update(p);

                    saved++;
                }

                await _db.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResult($"Approved {saved} high-confidence mappings"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving all high-confidence suggestions");
                return StatusCode(500, ApiResponse.ErrorResult("Internal server error"));
            }
        }

        [HttpGet("check-mappings/{sessionId}")]
        public async Task<IActionResult> CheckExistingMappings(string sessionId)
        {
            try
            {
                var session = await _db.ServiceCodesMappingSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return NotFound(new { success = false, message = "Session not found" });

                var providerItems = await _db.ProviderServiceItems
                    .AsNoTracking()
                    .Where(p => p.ServiceCodesMappingSessionId == session.Id)
                    .Select(p => new { p.HealthProviderId, p.ItemRelation })
                    .ToListAsync();

                var healthProviderId = providerItems.FirstOrDefault()?.HealthProviderId;

                var itemRelations = providerItems.Select(p => p.ItemRelation).Distinct().ToList();

                var existingMappings = await _db.ServiceCodeMappings
                    .AsNoTracking()
                    .Where(m => m.HealthProviderId == healthProviderId && itemRelations.Contains(m.ProviderItemRelation))
                    .Select(m => new
                    {
                        m.ProviderItemRelation,
                        m.NphiesServiceCodeValue,
                        m.ConfidenceScore
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = existingMappings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing mappings for session: {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "An error occurred while checking existing mappings" });
            }
        }

        private async Task<CreateServiceMappingSessionResponseDto> CreateSessionInternal(Guid healthProviderId, List<ExcelServiceImportDto> items, string? originalFileName)
        {
            var session = new ServiceCodesMappingSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                HealthProviderId = healthProviderId,
                OriginalFileName = originalFileName ?? string.Empty,
                TotalRows = items.Count,
                Status = "Processing"
            };

            _db.ServiceCodesMappingSessions.Add(session);
            await _db.SaveChangesAsync();

            // Build ProviderServiceItems and optionally pre-fill suggestions if provided
            var providerItems = new List<ProviderServiceItem>(items.Count);
            foreach (var row in items)
            {
                providerItems.Add(new ProviderServiceItem
                {
                    ServiceCodesMappingSessionId = session.Id,
                    HealthProviderId = healthProviderId,
                    ItemId = string.IsNullOrWhiteSpace(row.ItemId) ? null : row.ItemId,
                    ItemRelation = row.ItemRelation,
                    Name = row.Name ?? string.Empty,
                    NphiesCode = string.IsNullOrWhiteSpace(row.NphiesCode) ? null : row.NphiesCode,
                    NphiesDescription = string.IsNullOrWhiteSpace(row.NphiesDescription) ? null : row.NphiesDescription,
                    IsMapped = false
                });
            }

            _db.ProviderServiceItems.AddRange(providerItems);
            await _db.SaveChangesAsync();

            // Update session status
            session.Status = "Ready";
            session.ProcessedRows = 0;
            await _db.SaveChangesAsync();

            return new CreateServiceMappingSessionResponseDto
            {
                SessionId = session.SessionId,
                TotalRows = session.TotalRows
            };
        }
    }
}