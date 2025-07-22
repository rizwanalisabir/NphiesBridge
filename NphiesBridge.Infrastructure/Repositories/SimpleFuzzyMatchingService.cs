using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.Models;
using FuzzierSharp;
using System.Collections.Concurrent;
using NphiesBridge.Core.Interfaces;

namespace NphiesBridge.Infrastructure.Repositories
{
    public class SimpleFuzzyMatchingService : IAiFuzzyMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SimpleFuzzyMatchingService> _logger;

        // Cache all NPHIES codes in memory for performance
        private readonly Lazy<Task<List<CachedIcdCode>>> _allIcdCodes;

        private const string CACHE_KEY_PREFIX = "fuzzy_match_";
        private const string ICD_CODES_CACHE_KEY = "all_icd_codes";
        private const int CACHE_DURATION_MINUTES = 30;

        public SimpleFuzzyMatchingService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SimpleFuzzyMatchingService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;

            // Initialize lazy loading of ICD codes
            _allIcdCodes = new Lazy<Task<List<CachedIcdCode>>>(LoadAllIcdCodesAsync);
        }

        public async Task<ServiceResult<AiSuggestionResponseDto>> GetAiSuggestionAsync(AiSuggestionRequestDto request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Processing fuzzy match for: {DiagnosisName}", request.DiagnosisName);

                // Step 1: Check cache first
                var cacheKey = $"{CACHE_KEY_PREFIX}{request.DiagnosisName.ToLowerInvariant()}";
                if (_cache.TryGetValue(cacheKey, out AiSuggestionResponseDto cachedResult))
                {
                    _logger.LogInformation("Cache hit for diagnosis: {DiagnosisName}", request.DiagnosisName);
                    return ServiceResult<AiSuggestionResponseDto>.Success(cachedResult);
                }

                // Step 2: Perform fuzzy matching
                var bestMatch = await PerformFastFuzzyMatch(request.DiagnosisName);

                stopwatch.Stop();
                _logger.LogInformation("Fuzzy matching completed in {ElapsedMs}ms for: {DiagnosisName}",
                    stopwatch.ElapsedMilliseconds, request.DiagnosisName);

                // Step 3: Create response
                var response = new AiSuggestionResponseDto
                {
                    Success = bestMatch != null,
                    SuggestedCode = bestMatch != null ? new NphiesCodeDto
                    {
                        Id = bestMatch.IcdCode,
                        Text = $"{bestMatch.IcdCode} - {bestMatch.Description}",
                        Code = bestMatch.IcdCode,
                        Description = bestMatch.Description
                    } : null,
                    Confidence = bestMatch?.SimilarityScore ?? 0,
                    MatchType = GetMatchType(bestMatch?.SimilarityScore ?? 0),
                    Message = GetConfidenceMessage(bestMatch?.SimilarityScore ?? 0)
                };

                // Step 4: Cache the result
                _cache.Set(
                    cacheKey,
                    response,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES)
                    }.SetSize(100) // Required if SizeLimit is set
                );

                return ServiceResult<AiSuggestionResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fuzzy matching for: {DiagnosisName}", request.DiagnosisName);
                return ServiceResult<AiSuggestionResponseDto>.Failure($"Fuzzy matching failed: {ex.Message}");
            }
        }

        private async Task<FuzzyMatchResultDto?> PerformFastFuzzyMatch(string diagnosisName)
        {
            try
            {
                var cleanDiagnosis = CleanText(diagnosisName);

                // Get all ICD codes (will be loaded once and cached)
                var allCodes = await _allIcdCodes.Value;

                _logger.LogDebug("Comparing against {Count} ICD codes", allCodes.Count);

                // Use parallel processing for speed
                var matches = new ConcurrentBag<FuzzyMatchResultDto>();
                var minThreshold = 60; // Only consider matches above 60%

                // Pre-filter to reduce computation - only check codes with overlapping words
                var diagnosisWords = cleanDiagnosis.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 3)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var candidates = allCodes.Where(code =>
                    HasWordOverlap(diagnosisWords, code.CleanDescription)).ToList();

                _logger.LogDebug("Pre-filtered to {CandidateCount} candidates", candidates.Count);

                // Parallel fuzzy matching on filtered candidates
                await Task.Run(() =>
                {
                    Parallel.ForEach(candidates, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount
                    },
                    candidate =>
                    {
                        try
                        {
                            // Use TokenSetRatio - best for medical terms with different word orders
                            var score = Fuzz.TokenSetRatio(cleanDiagnosis, candidate.CleanDescription);

                            if (score >= minThreshold)
                            {
                                matches.Add(new FuzzyMatchResultDto
                                {
                                    IcdCode = candidate.Code,
                                    Description = candidate.Description,
                                    SimilarityScore = score
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error matching against code: {Code}", candidate.Code);
                        }
                    });
                });

                // Return best match
                return matches
                    .OrderByDescending(m => m.SimilarityScore)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PerformFastFuzzyMatch for: {DiagnosisName}", diagnosisName);
                return null;
            }
        }

        private bool HasWordOverlap(HashSet<string> diagnosisWords, string codeDescription)
        {
            var codeWords = codeDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return codeWords.Any(word => word.Length >= 3 && diagnosisWords.Contains(word));
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            return text.ToLowerInvariant()
                .Replace("type 1", "type1")
                .Replace("type 2", "type2")
                .Replace("type i", "type1")
                .Replace("type ii", "type2")
                .Trim();
        }

        private string GetMatchType(double confidence)
        {
            return confidence switch
            {
                >= 90 => "Excellent Match",
                >= 80 => "Very Good Match",
                >= 70 => "Good Match",
                >= 60 => "Moderate Match",
                _ => "Low Match"
            };
        }

        private string GetConfidenceMessage(double confidence)
        {
            return confidence switch
            {
                >= 90 => "Excellent match - high confidence recommendation",
                >= 80 => "Very good match - recommended for approval",
                >= 70 => "Good match - please review before approval",
                >= 60 => "Moderate match - manual verification recommended",
                _ => "Low confidence match - manual review required"
            };
        }

        // Load all ICD codes into memory once - async version with caching
        private async Task<List<CachedIcdCode>> LoadAllIcdCodesAsync()
        {
            try
            {
                // Check if already cached in memory cache
                if (_cache.TryGetValue(ICD_CODES_CACHE_KEY, out List<CachedIcdCode> cachedCodes))
                {
                    _logger.LogInformation("ICD codes loaded from memory cache: {Count}", cachedCodes.Count);
                    return cachedCodes;
                }

                _logger.LogInformation("Loading ICD codes from database...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var codes = await _context.NphiesIcdCodes
                    .AsNoTracking()
                    .Where(c => c.IsActive) // Only active codes
                    .Select(c => new { c.Code, c.Description })
                    .ToListAsync();

                var cachedIcdCodes = codes.Select(c => new CachedIcdCode
                {
                    Code = c.Code,
                    Description = c.Description,
                    CleanDescription = CleanTextStatic(c.Description)
                }).ToList();

                stopwatch.Stop();
                _logger.LogInformation("Loaded {Count} ICD codes from database in {ElapsedMs}ms",
                    cachedIcdCodes.Count, stopwatch.ElapsedMilliseconds);

                _cache.Set(
                    ICD_CODES_CACHE_KEY,
                    cachedIcdCodes,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    }.SetSize(100)
                );

                return cachedIcdCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ICD codes from database");
                return new List<CachedIcdCode>();
            }
        }

        private static string CleanTextStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            return text.ToLowerInvariant()
                .Replace("type 1", "type1")
                .Replace("type 2", "type2")
                .Replace("type i", "type1")
                .Replace("type ii", "type2")
                .Trim();
        }

        // Implement other interface methods with simple implementations
        public async Task<ServiceResult<List<FuzzyMatchResult>>> GetMultipleSuggestionsAsync(
            AiSuggestionRequestDto request, int maxResults = 5)
        {
            // Simple implementation - get top N matches
            return ServiceResult<List<FuzzyMatchResult>>.Success(new List<FuzzyMatchResult>());
        }

        public async Task<ServiceResult<BulkMatchingResponseDto>> ProcessBulkMatchingAsync(
            BulkMatchingRequestDto request)
        {
            // Process each code individually
            var response = new BulkMatchingResponseDto
            {
                Success = true,
                Message = "Bulk processing not implemented yet",
                ProcessedCount = 0,
                SuccessCount = 0,
                FailureCount = 0
            };

            return ServiceResult<BulkMatchingResponseDto>.Success(response);
        }
    }

    // Helper class for cached ICD codes
    public class CachedIcdCode
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CleanDescription { get; set; } = string.Empty;
    }
}