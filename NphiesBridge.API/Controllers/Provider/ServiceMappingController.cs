using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Core.Entities.ServiceMapping;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Infrastructure.Data;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceMappingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ServiceMappingController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("create-session")]
        public IActionResult CreateSession([FromBody] CreateServiceSessionRequestDto request)
        {
            // Validate and create a new ServiceMappingSession and associated HealthProviderServiceCodes
            // ... (implementation)
            return Ok(new CreateServiceSessionResponseDto { SessionId = Guid.NewGuid().ToString(), TotalRows = request.HealthProviderServiceCodes.Count, Status = "Created" });
        }

        [HttpGet("session/{id}")]
        public IActionResult GetSession(string id)
        {
            // Fetch session, provider items, stats, etc.
            // ... (implementation)
            return Ok(new ServiceMappingPageDto { SessionId = id, TotalRows = 0 });
        }

        //[HttpPost("save-mapping")]
        //public IActionResult SaveMapping([FromBody] SaveServiceMappingRequest request)
        //{
        //    //Save or update a ServiceCodeMapping record
        //    // ... (implementation)
        //    return Ok(new SuccessResponse { Success = true, Message = "Mapping saved." });
        //}

        [HttpPost("ai-suggestion")]
        public IActionResult AiSuggestion([FromBody] ServiceAiSuggestionRequestDto request)
        {
            // AI fuzzy matching logic using NphiesServiceCodes
            // ... (implementation)
            return Ok(new ServiceAiSuggestionResponseDto { Success = true, Confidence = 90 });
        }

        [HttpPost("bulk-match")]
        public IActionResult BulkMatch([FromBody] BulkServiceMatchingRequest request)
        {
            // Bulk fuzzy match for all unmapped provider items in session
            // ... (implementation)
            return Ok(new BulkServiceMatchingResponse { Success = true });
        }

        [HttpGet("statistics/{sessionId}")]
        public IActionResult GetStatistics(string sessionId)
        {
            // Return mapping completion stats
            // ... (implementation)
            return Ok(new ServiceMappingStatisticsDto { SessionId = sessionId });
        }

        [HttpGet("export")]
        public IActionResult ExportMappings([FromQuery] ExportServiceMappingsRequest request)
        {
            // Export mapped/unmapped records, e.g. Excel file
            // ... (implementation)
            return File(new byte[0], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceMappings.xlsx");
        }
    }
}