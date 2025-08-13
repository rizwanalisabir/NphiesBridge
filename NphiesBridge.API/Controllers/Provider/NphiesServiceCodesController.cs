using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<ApiResponse<NphiesServiceCodeDto>> GetByCode(string code)
        {
            var entity = _db.NphiesServiceCodes.FirstOrDefault(x => x.NphiesServiceCodeValue == code && !x.IsDeleted);
            if (entity == null)
                return ApiResponse<NphiesServiceCodeDto>.ErrorResult("Not found");

            var dto = new NphiesServiceCodeDto
            {
                Code = entity.NphiesServiceCodeValue,
                Description = entity.NphiesServiceDescription ?? ""
            };
            return ApiResponse<NphiesServiceCodeDto>.SuccessResult(dto);
        }
    }
}