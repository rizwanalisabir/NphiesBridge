using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System.Linq;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
    public class NphiesServiceCodesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NphiesServiceCodesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("search")]
        public ActionResult<ApiResponse<List<ServiceSearchResultDto>>> Search([FromQuery] ServiceSearchRequestDto request)
        {
            var q = (request.Query ?? string.Empty).Trim().ToLower();
            var codes = _db.NphiesServiceCodes
                .Where(x => (request.ActiveOnly ? !x.IsDeleted : true) &&
                            (string.IsNullOrEmpty(q) ||
                             x.NphiesServiceCodeValue.ToLower().Contains(q) ||
                             (x.NphiesServiceDescription ?? "").ToLower().Contains(q)))
                .OrderBy(x => x.NphiesServiceCodeValue)
                .Take(request.Limit)
                .Select(x => new ServiceSearchResultDto
                {
                    NphiesServiceCode = x.NphiesServiceCodeValue,
                    NphiesServiceDescription = x.NphiesServiceDescription ?? "",
                    IsActive = !x.IsDeleted
                })
                .ToList();

            return ApiResponse<List<ServiceSearchResultDto>>.SuccessResult(codes);
        }

        [HttpGet("{code}")]
        public ActionResult<ApiResponse<List<NphiesServiceCodeDto>>> GetByCode(string code)
        {
            // Use Where to filter the records based on the code, and then
            // use Select to project the results into NphiesServiceCodeDto objects.
            // ToList() executes the query and converts the result to a list.
            var dtoList = _db.NphiesServiceCodes
                .Where(x => x.NphiesServiceDescription.ToLower().Contains(code.ToLower()) && !x.IsDeleted)
                .Select(x => new NphiesServiceCodeDto
                {
                    Code = x.NphiesServiceCodeValue,
                    Description = x.NphiesServiceDescription ?? ""
                })
                .ToList();

            // If no matching records are found, return an error.
            if (dtoList == null || !dtoList.Any())
            {
                return ApiResponse<List<NphiesServiceCodeDto>>.ErrorResult("No matching service codes found.");
            }

            // Return the list of DTOs within a success response.
            return ApiResponse<List<NphiesServiceCodeDto>>.SuccessResult(dtoList);
        }

        // GET /api/nphiesservicecodes
        // Returns a flat array (not enveloped) to match existing JS:
        // nphiesServiceCodes = await response.json();
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int take = 10000, CancellationToken cancellationToken = default)
        {
            try
            {
                // Guardrail to avoid returning excessive data by accident
                if (take <= 0 || take > 50000) take = 10000;

                var list = await _db.NphiesServiceCodes
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.NphiesServiceCodeValue)
                    .Take(take)
                    .Select(x => new NphiesServiceCodeDto
                    {
                        Code = x.NphiesServiceCodeValue,
                        Description = x.NphiesServiceDescription ?? string.Empty
                        // If your DTO has more fields (e.g., Category, Chapter), map them here.
                    })
                    .ToListAsync(cancellationToken);

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new object[0]);
            }
        }
    }
}