using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace NphiesBridge.Infrastructure.Repositories
{
    /// <summary>
    /// AI fuzzy matching service for Service Mapping (replica of ICD logic)
    /// </summary>
    public class SimpleFuzzyServiceMappingService : IAiFuzzyServiceMappingService
    {
        private readonly ApplicationDbContext _db;

        public SimpleFuzzyServiceMappingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceAiSuggestionResponseDto> GetAiSuggestionAsync(ServiceAiSuggestionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.HealthProviderServiceName))
                return new ServiceAiSuggestionResponseDto
                {
                    Success = false,
                    Confidence = 0,
                    Message = "Service name is required"
                };

            var nphiesCodes = await _db.NphiesServiceCodes
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var bestMatch = nphiesCodes
                .Select(x => new
                {
                    Code = x.NphiesServiceCodeValue,
                    Description = x.NphiesServiceDescription ?? "",
                    Confidence = GetFuzzyConfidence(request.HealthProviderServiceName, x.NphiesServiceDescription ?? "")
                })
                .OrderByDescending(x => x.Confidence)
                .FirstOrDefault();

            if (bestMatch == null || bestMatch.Confidence < 60)
            {
                return new ServiceAiSuggestionResponseDto
                {
                    Success = false,
                    Confidence = 0,
                    MatchType = "No good match",
                    Message = "No suitable NPHIES service code found."
                };
            }

            return new ServiceAiSuggestionResponseDto
            {
                Success = true,
                Confidence = bestMatch.Confidence,
                MatchType = "Description similarity",
                SuggestedCode = new NphiesServiceCodeDto
                {
                    Code = bestMatch.Code,
                    Description = bestMatch.Description
                },
                Message = $"Best match is {bestMatch.Code} ({bestMatch.Description})"
            };
        }

        public async Task<List<ServiceAiMatchSuggestionDto>> BulkSuggestAsync(Guid sessionId, int batchSize = 50)
        {
            var session = await _db.ServiceMappingSessions
                .Include(x => x.HealthProviderServiceCodes)
                .FirstOrDefaultAsync(x => x.Id == sessionId);

            if (session == null)
                return new List<ServiceAiMatchSuggestionDto>();

            var nphiesCodes = await _db.NphiesServiceCodes.Where(x => !x.IsDeleted).ToListAsync();

            var results = new List<ServiceAiMatchSuggestionDto>();
            foreach (var svc in session.HealthProviderServiceCodes.Where(x => !x.IsMapped).Take(batchSize))
            {
                var bestMatch = nphiesCodes
                    .Select(x => new
                    {
                        Code = x.NphiesServiceCodeValue,
                        Description = x.NphiesServiceDescription ?? "",
                        Confidence = GetFuzzyConfidence(svc.HealthProviderServiceName, x.NphiesServiceDescription ?? "")
                    })
                    .OrderByDescending(x => x.Confidence)
                    .FirstOrDefault();

                if (bestMatch != null && bestMatch.Confidence >= 70)
                {
                    results.Add(new ServiceAiMatchSuggestionDto
                    {
                        HealthProviderServiceCodeId = svc.Id,
                        NphiesServiceCode = bestMatch.Code,
                        NphiesDescription = bestMatch.Description,
                        ConfidenceScore = bestMatch.Confidence.ToString(),
                        MatchReason = "AI Fuzzy match"
                    });
                }
            }

            return results;
        }

        // --- Fuzzy Matching Logic ---

        private int GetFuzzyConfidence(string input, string target)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target)) return 0;
            input = input.ToLower().Trim();
            target = target.ToLower().Trim();

            if (input == target) return 100;
            if (target.Contains(input) || input.Contains(target)) return 90;

            // Simple Levenshtein distance (replica of ICD logic)
            int distance = LevenshteinDistance(input, target);
            int maxLen = Math.Max(input.Length, target.Length);
            if (maxLen == 0) return 0;
            int percent = 100 - (int)((double)distance / maxLen * 100);
            return percent;
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}