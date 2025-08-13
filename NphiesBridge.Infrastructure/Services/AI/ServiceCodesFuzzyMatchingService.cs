using FuzzierSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using System.Collections.Concurrent;

namespace NphiesBridge.Infrastructure.Services.AI
{
    // Mirrors your ICD SimpleFuzzyMatchingService: pre-filter + Parallel.ForEach + FuzzierSharp
    public class ServiceCodesFuzzyMatchingService : IAiServiceMatchingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ServiceCodesFuzzyMatchingService> _logger;
        private readonly int _highConfidence;

        private const string CorpusCacheKey = "service_codes_fuzzy_corpus";
        private const string ResultCachePrefix = "service_codes_fuzzy_result_";
        private const int CacheMinutes = 30;

        public ServiceCodesFuzzyMatchingService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<ServiceCodesFuzzyMatchingService> logger,
            IConfiguration config)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
            var thresholdStr = config["ServiceCodeMapping:HighConfidenceThreshold"];
            _highConfidence = int.TryParse(thresholdStr, out var parsed) ? parsed : 90;
        }

        public async Task<ServiceResult<ServiceAiSuggestionResponseDto>> GetAiSuggestionAsync(ServiceAiSuggestionRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ServiceAiSuggestionResponseDto>.Failure("Name is required for AI matching.");

                var cacheKey = $"{ResultCachePrefix}{request.Name.ToLowerInvariant()}";
                if (_cache.TryGetValue(cacheKey, out ServiceAiSuggestionResponseDto cached))
                    return ServiceResult<ServiceAiSuggestionResponseDto>.Success(cached);

                var corpus = await GetCorpusAsync(ct);
                if (corpus.Count == 0)
                    return ServiceResult<ServiceAiSuggestionResponseDto>.Failure("NPHIES service codes corpus is empty.");

                var best = await PerformFastFuzzyMatch(request.Name, corpus);
                if (best == null)
                    return ServiceResult<ServiceAiSuggestionResponseDto>.Failure("No matches found.");

                var response = new ServiceAiSuggestionResponseDto
                {
                    SuggestedNphiesCode = best.Code,
                    SuggestedDescription = best.Description,
                    ConfidenceScore = best.Score,
                    MatchReason = best.Reason
                };

                _cache.Set(cacheKey, response, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes)
                }.SetSize(100));

                return ServiceResult<ServiceAiSuggestionResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in service codes fuzzy matching for: {Name}", request?.Name);
                return ServiceResult<ServiceAiSuggestionResponseDto>.Failure("Matching failed.");
            }
        }

        public async Task<ServiceResult<List<ServiceAiSuggestionResponseDto>>> GetMultipleSuggestionsAsync(ServiceAiSuggestionRequestDto request, int maxResults = 5, CancellationToken ct = default)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<List<ServiceAiSuggestionResponseDto>>.Failure("Name is required for AI matching.");

                var corpus = await GetCorpusAsync(ct);
                if (corpus.Count == 0)
                    return ServiceResult<List<ServiceAiSuggestionResponseDto>>.Failure("NPHIES service codes corpus is empty.");

                var ranked = await RankMatchesParallel(request.Name, corpus, maxResults);

                return ServiceResult<List<ServiceAiSuggestionResponseDto>>.Success(ranked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple fuzzy suggestions for: {Name}", request?.Name);
                return ServiceResult<List<ServiceAiSuggestionResponseDto>>.Failure("Matching failed.");
            }
        }

        public async Task<ServiceResult<BulkServiceMatchingResponseDto>> ProcessBulkMatchingAsync(BulkServiceMatchingRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request == null || request.Items == null || request.Items.Count == 0)
                    return ServiceResult<BulkServiceMatchingResponseDto>.Failure("No items to process.");

                var corpus = await GetCorpusAsync(ct);
                if (corpus.Count == 0)
                    return ServiceResult<BulkServiceMatchingResponseDto>.Failure("NPHIES service codes corpus is empty.");

                var resp = new BulkServiceMatchingResponseDto();

                foreach (var item in request.Items)
                {
                    var suggestions = new List<ServiceAiSuggestionResponseDto>();
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        suggestions = await RankMatchesParallel(item.Name, corpus, request.TopN);
                    }

                    resp.Results.Add(new BulkServiceMatchingResultItemDto
                    {
                        ProviderServiceItemId = item.ProviderServiceItemId,
                        InputName = item.Name ?? string.Empty,
                        InputItemId = item.ItemId,
                        InputItemRelation = item.ItemRelation,
                        Suggestions = suggestions
                    });
                }

                resp.TotalProcessed = resp.Results.Count;
                return ServiceResult<BulkServiceMatchingResponseDto>.Success(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk fuzzy matching for services");
                return ServiceResult<BulkServiceMatchingResponseDto>.Failure("Bulk matching failed.");
            }
        }

        private async Task<List<CorpusEntry>> GetCorpusAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(CorpusCacheKey, out List<CorpusEntry>? cached) && cached != null)
                return cached;

            var rows = await _db.NphiesServiceCodes
                .AsNoTracking()
                .Select(x => new CorpusEntry
                {
                    Code = x.NphiesServiceCodeValue,
                    Description = x.NphiesServiceDescription ?? string.Empty,
                    CleanDescription = CleanText(x.NphiesServiceDescription ?? string.Empty)
                })
                .ToListAsync(ct);

            _cache.Set(CorpusCacheKey, rows, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes)
            }.SetSize(Math.Max(1, rows.Count)));

            _logger.LogInformation("Loaded NphiesServiceCodes corpus: {Count} rows", rows.Count);
            return rows;
        }

        private async Task<ScoredCandidate?> PerformFastFuzzyMatch(string name, List<CorpusEntry> corpus)
        {
            var cleanName = CleanText(name);

            // Pre-filter: only candidates with word overlap (>=3 letters) to reduce work
            var nameWords = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length >= 3)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var candidates = corpus.Where(c => HasWordOverlap(nameWords, c.CleanDescription)).ToList();

            var matches = new ConcurrentBag<ScoredCandidate>();
            var minThreshold = 60;

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
                        // Use TokenSetRatio as in your ICD implementation
                        var score = Fuzz.TokenSetRatio(cleanName, candidate.CleanDescription);

                        if (score >= minThreshold)
                        {
                            var reason =
                                score >= _highConfidence ? "High similarity match" :
                                "TokenSetRatio similarity";

                            matches.Add(new ScoredCandidate(candidate.Code, candidate.Description, score, reason));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error matching against service code: {Code}", candidate.Code);
                    }
                });
            });

            return matches
                .OrderByDescending(m => m.Score)
                .FirstOrDefault();
        }

        private async Task<List<ServiceAiSuggestionResponseDto>> RankMatchesParallel(string name, List<CorpusEntry> corpus, int topN)
        {
            var cleanName = CleanText(name);

            var nameWords = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                     .Where(w => w.Length >= 3)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var candidates = corpus.Where(c => HasWordOverlap(nameWords, c.CleanDescription)).ToList();

            var results = new ConcurrentBag<ScoredCandidate>();
            var minThreshold = 40; // broader for a list

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
                        // Consider multiple fuzz metrics; take best like ICD style
                        var tokenSet = Fuzz.TokenSetRatio(cleanName, candidate.CleanDescription);
                        var tokenSort = Fuzz.TokenSortRatio(cleanName, candidate.CleanDescription);
                        var partial = Fuzz.PartialRatio(cleanName, candidate.CleanDescription);

                        var score = Math.Max(tokenSet, Math.Max(tokenSort, partial));

                        if (score >= minThreshold)
                        {
                            var reason =
                                score >= _highConfidence ? "High similarity match" :
                                tokenSet >= tokenSort && tokenSet >= partial ? "TokenSetRatio" :
                                tokenSort >= partial ? "TokenSortRatio" : "PartialRatio";

                            results.Add(new ScoredCandidate(candidate.Code, candidate.Description, score, reason));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error scoring candidate: {Code}", candidate.Code);
                    }
                });
            });

            return results
                .OrderByDescending(r => r.Score)
                .Take(Math.Max(1, topN))
                .Select(s => new ServiceAiSuggestionResponseDto
                {
                    SuggestedNphiesCode = s.Code,
                    SuggestedDescription = s.Description,
                    ConfidenceScore = s.Score,
                    MatchReason = s.Reason
                })
                .ToList();
        }

        private static bool HasWordOverlap(HashSet<string> words, string description)
        {
            var codeWords = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return codeWords.Any(word => word.Length >= 3 && words.Contains(word));
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text.ToLowerInvariant()
                .Replace("type 1", "type1")
                .Replace("type 2", "type2")
                .Replace("type i", "type1")
                .Replace("type ii", "type2")
                .Replace("-", " ")
                .Replace("_", " ")
                .Trim();
        }

        private sealed record CorpusEntry
        {
            public string Code { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CleanDescription { get; init; } = string.Empty;
        }

        private sealed record ScoredCandidate(string Code, string Description, int Score, string Reason);
    }
}