using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System.Text.Json;

namespace NphiesBridge.API.Controllers.Provider
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NphiesCodesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NphiesCodesController> _logger;
        private const string NPHIES_CODES_CACHE_KEY = "nphies_codes_all";
        private const int CACHE_DURATION_HOURS = 24;

        public NphiesCodesController(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<NphiesCodesController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get all NPHIES ICD codes for Select2 dropdown (44K+ codes)
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour in browser
        public async Task<ActionResult<List<NphiesCodeDto>>> GetAllNphiesCodes()
        {
            try
            {
                _logger.LogInformation("Fetching all NPHIES codes for Select2 dropdown");

                // Try to get from cache first
                if (_cache.TryGetValue(NPHIES_CODES_CACHE_KEY, out List<NphiesCodeDto>? cachedCodes))
                {
                    _logger.LogInformation($"Returning {cachedCodes?.Count} NPHIES codes from cache");
                    return Ok(cachedCodes);
                }

                // If not in cache, fetch from database
                var nphiesCodes = await _context.NphiesIcdCodes
                    .Where(n => n.IsActive && !n.IsDeleted)
                    .OrderBy(n => n.Code)
                    .Select(n => new NphiesCodeDto
                    {
                        Id = n.Code,
                        Text = $"{n.Code} - {n.Description}",
                        Code = n.Code,
                        Description = n.Description,
                        Category = n.Category,
                        Chapter = n.Chapter
                    }).ToListAsync();

                // Cache the results
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CACHE_DURATION_HOURS),
                    Priority = CacheItemPriority.High,
                    Size = 15_360 // Adjust based on your memory cache size limits
                };

                _cache.Set(NPHIES_CODES_CACHE_KEY, nphiesCodes, cacheOptions);

                _logger.LogInformation($"Fetched and cached {nphiesCodes.Count} NPHIES codes from database");

                return Ok(nphiesCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching NPHIES codes");
                return StatusCode(500, new { message = "Internal server error while fetching NPHIES codes" });
            }
        }

        [HttpGet("search-code")]
        public async Task<ActionResult<List<NphiesCodeDto>>> SearchNphiesCodes(
    [FromQuery] string q,
    [FromQuery] int limit = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
                {
                    return Ok(new List<NphiesCodeDto>());
                }

                var query = q.ToLowerInvariant().Trim();

                var results = await _context.NphiesIcdCodes
                    .AsNoTracking()
                    .Where(code => code.IsActive &&
                           (code.Code.ToLower().Contains(query) ||
                            code.Description.ToLower().Contains(query)))
                    .OrderBy(code => code.Code.ToLower().StartsWith(query) ? 0 : 1)
                    .ThenBy(code => code.Code)
                    .Take(limit)
                    .Select(code => new NphiesCodeDto
                    {
                        Id = code.Code,
                        Code = code.Code,
                        Text = $"{code.Code} - {code.Description}",
                        Description = code.Description
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching NPHIES codes");
                return StatusCode(500, "Search failed");
            }
        }

        /// <summary>
        /// Search NPHIES codes with pagination and filtering
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<object>> SearchNphiesCodes(
            [FromQuery] string? term = null,
            [FromQuery] string? category = null,
            [FromQuery] string? chapter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (pageSize > 100) pageSize = 100; // Limit page size
                if (page < 1) page = 1;

                var query = _context.NphiesIcdCodes
                    .Where(n => n.IsActive && !n.IsDeleted);

                // Apply search term filter
                if (!string.IsNullOrWhiteSpace(term))
                {
                    var searchTerm = term.ToLower().Trim();
                    query = query.Where(n =>
                        n.Code.ToLower().Contains(searchTerm) ||
                        n.Description.ToLower().Contains(searchTerm));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(n => n.Category == category);
                }

                // Apply chapter filter
                if (!string.IsNullOrWhiteSpace(chapter))
                {
                    query = query.Where(n => n.Chapter == chapter);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var codes = await query
                    .OrderBy(n => n.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NphiesCodeDto
                    {
                        Id = n.Code,
                        Text = $"{n.Code} - {n.Description}",
                        Code = n.Code,
                        Description = n.Description,
                        Category = n.Category,
                        Chapter = n.Chapter
                    })
                    .ToListAsync();

                var result = new
                {
                    codes,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching NPHIES codes with term: {SearchTerm}", term);
                return StatusCode(500, new { message = "Internal server error while searching NPHIES codes" });
            }
        }

        /// <summary>
        /// Get NPHIES code by specific code
        /// </summary>
        [HttpGet("{code}")]
        public async Task<ActionResult<NphiesCodeDto>> GetNphiesCodeByCode(string code)
        {
            try
            {
                var nphiesCode = await _context.NphiesIcdCodes
                    .Where(n => n.Code == code && n.IsActive && !n.IsDeleted)
                    .Select(n => new NphiesCodeDto
                    {
                        Id = n.Code,
                        Text = $"{n.Code} - {n.Description}",
                        Code = n.Code,
                        Description = n.Description,
                        Category = n.Category,
                        Chapter = n.Chapter
                    })
                    .FirstOrDefaultAsync();

                if (nphiesCode == null)
                {
                    return NotFound(new { message = $"NPHIES code '{code}' not found" });
                }

                return Ok(nphiesCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching NPHIES code: {Code}", code);
                return StatusCode(500, new { message = "Internal server error while fetching NPHIES code" });
            }
        }

        /// <summary>
        /// Get categories for filtering
        /// </summary>
        [HttpGet("categories")]
        [ResponseCache(Duration = 7200)] // Cache for 2 hours
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.NphiesIcdCodes
                    .Where(n => n.IsActive && !n.IsDeleted && !string.IsNullOrEmpty(n.Category))
                    .Select(n => n.Category!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching NPHIES categories");
                return StatusCode(500, new { message = "Internal server error while fetching categories" });
            }
        }

        /// <summary>
        /// Get chapters for filtering
        /// </summary>
        [HttpGet("chapters")]
        [ResponseCache(Duration = 7200)] // Cache for 2 hours
        public async Task<ActionResult<List<string>>> GetChapters()
        {
            try
            {
                var chapters = await _context.NphiesIcdCodes
                    .Where(n => n.IsActive && !n.IsDeleted && !string.IsNullOrEmpty(n.Chapter))
                    .Select(n => n.Chapter!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching NPHIES chapters");
                return StatusCode(500, new { message = "Internal server error while fetching chapters" });
            }
        }

        /// <summary>
        /// Clear NPHIES codes cache (admin only)
        /// </summary>
        [HttpPost("clear-cache")]
        // [Authorize(Policy = "AdminOnly")] // Uncomment when you have auth
        public IActionResult ClearCache()
        {
            try
            {
                _cache.Remove(NPHIES_CODES_CACHE_KEY);
                _logger.LogInformation("NPHIES codes cache cleared by admin");

                return Ok(new { message = "Cache cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing NPHIES codes cache");
                return StatusCode(500, new { message = "Internal server error while clearing cache" });
            }
        }

        /// <summary>
        /// Get cache statistics (admin only)
        /// </summary>
        [HttpGet("cache-stats")]
        // [Authorize(Policy = "AdminOnly")] // Uncomment when you have auth
        public IActionResult GetCacheStats()
        {
            try
            {
                var isCached = _cache.TryGetValue(NPHIES_CODES_CACHE_KEY, out List<NphiesCodeDto>? cachedCodes);

                var stats = new
                {
                    isCached,
                    cachedCodesCount = cachedCodes?.Count ?? 0,
                    cacheKey = NPHIES_CODES_CACHE_KEY,
                    cacheDurationHours = CACHE_DURATION_HOURS
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return StatusCode(500, new { message = "Internal server error while getting cache stats" });
            }
        }

        /// <summary>
        /// Export NPHIES codes to JSON file (for backup/debugging)
        /// </summary>
        [HttpGet("export")]
        // [Authorize(Policy = "AdminOnly")] // Uncomment when you have auth
        public async Task<ActionResult> ExportNphiesCodesToJson()
        {
            try
            {
                var nphiesCodes = await _context.NphiesIcdCodes
                    .Where(n => n.IsActive && !n.IsDeleted)
                    .OrderBy(n => n.Code)
                    .Select(n => new NphiesCodeDto
                    {
                        Id = n.Code,
                        Text = $"{n.Code} - {n.Description}",
                        Code = n.Code,
                        Description = n.Description,
                        Category = n.Category,
                        Chapter = n.Chapter
                    })
                    .ToListAsync();

                var jsonContent = JsonSerializer.Serialize(nphiesCodes, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var fileName = $"nphies-codes-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);

                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting NPHIES codes to JSON");
                return StatusCode(500, new { message = "Internal server error while exporting NPHIES codes" });
            }
        }
    }
}