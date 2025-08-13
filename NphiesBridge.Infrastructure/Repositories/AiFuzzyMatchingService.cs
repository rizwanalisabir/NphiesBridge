// ============================================
// AI Fuzzy Matching Service Implementation
// File: NphiesBridge.Infrastructure.Repositories/AiFuzzyMatchingService.cs
// ============================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NphiesBridge.Core.Entities.IcdMapping;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.Models;
using System.Text.RegularExpressions;

namespace NphiesBridge.Infrastructure.Repositories
{
    public class AiFuzzyMatchingService : IAiFuzzyMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AiFuzzyMatchingService> _logger;

        private const string NPHIES_CODES_CACHE_KEY = "nphies_codes_for_matching";
        private const int CACHE_DURATION_HOURS = 12;

        // Medical keywords for enhanced matching
        private readonly Dictionary<string, List<string>> _medicalKeywordMappings = new()
        {
            { "diabetes", new List<string> { "diabetes", "diabetic", "dm", "mellitus" } },
            { "hypertension", new List<string> { "hypertension", "hypertensive", "htn", "high blood pressure" } },
            { "myocardial", new List<string> { "myocardial", "heart attack", "mi", "cardiac", "coronary" } },
            { "copd", new List<string> { "copd", "chronic obstructive", "pulmonary", "emphysema", "bronchitis" } },
            { "asthma", new List<string> { "asthma", "asthmatic", "bronchial", "wheeze", "allergic" } },
            { "pneumonia", new List<string> { "pneumonia", "pneumonic", "lung infection", "chest infection" } },
            { "fracture", new List<string> { "fracture", "break", "broken", "fx" } },
            { "infection", new List<string> { "infection", "sepsis", "bacterial", "viral", "inflammatory" } }
        };

        public AiFuzzyMatchingService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<AiFuzzyMatchingService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ServiceResult<AiSuggestionResponseDto>> GetAiSuggestionAsync(AiSuggestionRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing AI suggestion for hospital code: {HospitalCode}", request.HospitalCode);

                // Step 1: Direct ICD-10-AM code match (if provided by user)
                if (!string.IsNullOrWhiteSpace(request.SuggestedIcd10Am))
                {
                    var directMatch = await GetDirectCodeMatch(request.SuggestedIcd10Am);
                    if (directMatch != null)
                    {
                        _logger.LogInformation("Direct code match found for: {SuggestedIcd10Am}", request.SuggestedIcd10Am);
                        return ServiceResult<AiSuggestionResponseDto>.Success(new AiSuggestionResponseDto
                        {
                            Success = true,
                            SuggestedCode = directMatch,
                            Confidence = 100,
                            MatchType = "Direct Code Match",
                            Message = "Exact ICD-10-AM code match found"
                        });
                    }
                }

                // Step 2: Get candidate codes using pre-filtering
                var candidateCodes = await GetCandidateCodesAsync(request.DiagnosisName, request.DiagnosisDescription);

                if (!candidateCodes.Any())
                {
                    _logger.LogWarning("No candidate codes found for diagnosis: {DiagnosisName}", request.DiagnosisName);
                    return ServiceResult<AiSuggestionResponseDto>.Success(new AiSuggestionResponseDto
                    {
                        Success = false,
                        Confidence = 0,
                        Message = "No matching codes found in NPHIES database"
                    });
                }

                // Step 3: Perform fuzzy matching on candidates
                var fuzzyResults = await PerformFuzzyMatching(request.DiagnosisName, request.DiagnosisDescription, candidateCodes);

                if (!fuzzyResults.Any() || fuzzyResults[0].SimilarityScore < 40)
                {
                    return ServiceResult<AiSuggestionResponseDto>.Success(new AiSuggestionResponseDto
                    {
                        Success = false,
                        Confidence = 0,
                        Message = "No sufficiently similar codes found (minimum 40% similarity required)"
                    });
                }

                var bestMatch = fuzzyResults[0];
                var alternativeSuggestions = fuzzyResults.Skip(1).Take(3).Select(r => r.NphiesCode).ToList();

                var response = new AiSuggestionResponseDto
                {
                    Success = true,
                    SuggestedCode = bestMatch.NphiesCode,
                    Confidence = Math.Round(bestMatch.SimilarityScore, 1),
                    MatchType = bestMatch.MatchType,
                    Message = GetConfidenceMessage(bestMatch.SimilarityScore),
                    AlternativeSuggestions = alternativeSuggestions
                };

                _logger.LogInformation("AI suggestion completed. Best match: {Code} with {Confidence}% confidence",
                    bestMatch.NphiesCode.Code, bestMatch.SimilarityScore);

                return ServiceResult<AiSuggestionResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI suggestion for hospital code: {HospitalCode}", request.HospitalCode);
                return ServiceResult<AiSuggestionResponseDto>.Failure($"AI matching failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FuzzyMatchResult>>> GetMultipleSuggestionsAsync(AiSuggestionRequestDto request, int maxResults = 5)
        {
            try
            {
                var candidateCodes = await GetCandidateCodesAsync(request.DiagnosisName, request.DiagnosisDescription);
                var fuzzyResults = await PerformFuzzyMatching(request.DiagnosisName, request.DiagnosisDescription, candidateCodes);

                return ServiceResult<List<FuzzyMatchResult>>.Success(fuzzyResults.Take(maxResults).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple suggestions for: {DiagnosisName}", request.DiagnosisName);
                return ServiceResult<List<FuzzyMatchResult>>.Failure($"Failed to get multiple suggestions: {ex.Message}");
            }
        }

        public async Task<ServiceResult<BulkMatchingResponseDto>> ProcessBulkMatchingAsync(BulkMatchingRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing bulk matching for session: {SessionId}", request.SessionId);

                int successCount = 0;
                int failureCount = 0;
                var errors = new List<string>();

                foreach (var hospitalCode in request.HospitalCodes)
                {
                    try
                    {
                        var aiRequest = new AiSuggestionRequestDto
                        {
                            HospitalCodeId = hospitalCode.Id,
                            DiagnosisName = hospitalCode.DiagnosisName,
                            DiagnosisDescription = hospitalCode.DiagnosisDescription,
                            SuggestedIcd10Am = hospitalCode.SuggestedIcd10Am,
                            HospitalCode = hospitalCode.HospitalCode,
                            SessionId = request.SessionId
                        };

                        var result = await GetAiSuggestionAsync(aiRequest);

                        if (result.IsSuccess && result.Data?.Success == true)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            errors.Add($"Failed to match hospital code: {hospitalCode.HospitalCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        errors.Add($"Error processing {hospitalCode.HospitalCode}: {ex.Message}");
                        _logger.LogError(ex, "Error in bulk matching for hospital code: {Code}", hospitalCode.HospitalCode);
                    }
                }

                var response = new BulkMatchingResponseDto
                {
                    Success = successCount > 0,
                    Message = $"Processed {request.HospitalCodes.Count} codes. {successCount} successful, {failureCount} failed.",
                    ProcessedCount = request.HospitalCodes.Count,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Errors = errors
                };

                return ServiceResult<BulkMatchingResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk matching for session: {SessionId}", request.SessionId);
                return ServiceResult<BulkMatchingResponseDto>.Failure($"Bulk matching failed: {ex.Message}");
            }
        }

        // ============================================
        // PRIVATE HELPER METHODS
        // ============================================

        private async Task<NphiesCodeDto?> GetDirectCodeMatch(string suggestedIcd10Am)
        {
            try
            {
                var cleanCode = suggestedIcd10Am.Trim().ToUpper();

                var directMatch = await _context.NphiesIcdCodes
                    .Where(n => n.Code.ToUpper() == cleanCode && n.IsActive)
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

                return directMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct code match for: {SuggestedIcd10Am}", suggestedIcd10Am);
                return null;
            }
        }

        private async Task<List<NphiesIcdCode>> GetCandidateCodesAsync(string diagnosisName, string? diagnosisDescription = null)
        {
            try
            {
                // Try to get from cache first
                var cacheKey = $"candidates_{diagnosisName.ToLower().GetHashCode()}";
                if (_cache.TryGetValue(cacheKey, out List<NphiesIcdCode>? cachedCandidates))
                {
                    return cachedCandidates ?? new List<NphiesIcdCode>();
                }

                var candidates = new List<NphiesIcdCode>();
                var searchText = diagnosisName.ToLower().Trim();
                var searchWords = ExtractMedicalKeywords(searchText);

                // Step 1: Full-text search on description
                var fullTextCandidates = await _context.NphiesIcdCodes
                    .Where(n => n.IsActive)
                    .Where(n => EF.Functions.Contains(n.Description, searchText))
                    .Take(500)
                    .ToListAsync();

                candidates.AddRange(fullTextCandidates);

                // Step 2: Keyword-based search if full-text doesn't yield enough results
                if (candidates.Count < 100)
                {
                    foreach (var keyword in searchWords.Take(3)) // Limit to top 3 keywords for performance
                    {
                        var keywordCandidates = await _context.NphiesIcdCodes
                            .Where(n => n.IsActive)
                            .Where(n => n.Description.ToLower().Contains(keyword))
                            .Take(200)
                            .ToListAsync();

                        candidates.AddRange(keywordCandidates);
                    }
                }

                // Step 3: Medical synonym search
                var medicalSynonyms = GetMedicalSynonyms(searchText);
                foreach (var synonym in medicalSynonyms.Take(2))
                {
                    var synonymCandidates = await _context.NphiesIcdCodes
                        .Where(n => n.IsActive)
                        .Where(n => n.Description.ToLower().Contains(synonym))
                        .Take(100)
                        .ToListAsync();

                    candidates.AddRange(synonymCandidates);
                }

                // Remove duplicates and cache results
                var uniqueCandidates = candidates.DistinctBy(c => c.Id).ToList();

                _cache.Set(cacheKey, uniqueCandidates, TimeSpan.FromHours(2));

                _logger.LogInformation("Found {Count} candidate codes for diagnosis: {DiagnosisName}",
                    uniqueCandidates.Count, diagnosisName);

                return uniqueCandidates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate codes for: {DiagnosisName}", diagnosisName);
                return new List<NphiesIcdCode>();
            }
        }

        private async Task<List<FuzzyMatchResult>> PerformFuzzyMatching(
            string diagnosisName,
            string? diagnosisDescription,
            List<NphiesIcdCode> candidates)
        {
            try
            {
                var results = new List<FuzzyMatchResult>();
                var searchText = $"{diagnosisName} {diagnosisDescription}".ToLower().Trim();

                await Task.Run(() =>
                {
                    Parallel.ForEach(candidates, candidate =>
                    {
                        var score = CalculateComprehensiveSimilarity(searchText, candidate);

                        if (score.TotalScore >= 40) // Minimum threshold
                        {
                            var result = new FuzzyMatchResult
                            {
                                NphiesCode = new NphiesCodeDto
                                {
                                    Id = candidate.Code,
                                    Text = $"{candidate.Code} - {candidate.Description}",
                                    Code = candidate.Code,
                                    Description = candidate.Description,
                                    Category = candidate.Category,
                                    Chapter = candidate.Chapter
                                },
                                SimilarityScore = score.TotalScore,
                                MatchType = score.MatchType,
                                MatchDetails = score.Details
                            };

                            lock (results)
                            {
                                results.Add(result);
                            }
                        }
                    });
                });

                return results.OrderByDescending(r => r.SimilarityScore).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing fuzzy matching for: {DiagnosisName}", diagnosisName);
                return new List<FuzzyMatchResult>();
            }
        }

        private (double TotalScore, string MatchType, Dictionary<string, object> Details) CalculateComprehensiveSimilarity(
            string searchText,
            NphiesIcdCode candidate)
        {
            var candidateText = candidate.Description.ToLower();
            var details = new Dictionary<string, object>();

            // 1. Exact match (100%)
            if (candidateText.Equals(searchText, StringComparison.OrdinalIgnoreCase))
            {
                return (100.0, "Exact Match", new Dictionary<string, object> { { "type", "exact" } });
            }

            // 2. Levenshtein distance similarity (40% weight)
            var levenshteinScore = CalculateLevenshteinSimilarity(searchText, candidateText) * 0.4;
            details["levenshtein"] = Math.Round(levenshteinScore, 2);

            // 3. Keyword overlap similarity (35% weight)
            var keywordScore = CalculateKeywordSimilarity(searchText, candidateText) * 0.35;
            details["keyword"] = Math.Round(keywordScore, 2);

            // 4. Medical term bonus (15% weight)
            var medicalScore = CalculateMedicalTermBonus(searchText, candidateText) * 0.15;
            details["medical"] = Math.Round(medicalScore, 2);

            // 5. Length similarity bonus (10% weight)
            var lengthScore = CalculateLengthSimilarity(searchText, candidateText) * 0.1;
            details["length"] = Math.Round(lengthScore, 2);

            var totalScore = levenshteinScore + keywordScore + medicalScore + lengthScore;
            totalScore = Math.Min(99.0, totalScore); // Cap at 99% for non-exact matches

            var matchType = totalScore switch
            {
                >= 80 => "High Similarity",
                >= 60 => "Medium Similarity",
                >= 40 => "Low Similarity",
                _ => "Poor Match"
            };

            details["totalScore"] = Math.Round(totalScore, 2);

            return (totalScore, matchType, details);
        }

        // Additional helper methods for similarity calculations...
        private double CalculateLevenshteinSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            var distance = ComputeLevenshteinDistance(s1, s2);
            var maxLength = Math.Max(s1.Length, s2.Length);

            return maxLength == 0 ? 100 : (1.0 - (double)distance / maxLength) * 100;
        }

        private int ComputeLevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(
                        matrix[i - 1, j] + 1,      // deletion
                        matrix[i, j - 1] + 1),     // insertion
                        matrix[i - 1, j - 1] + cost); // substitution
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        private double CalculateKeywordSimilarity(string s1, string s2)
        {
            var words1 = ExtractMedicalKeywords(s1);
            var words2 = ExtractMedicalKeywords(s2);

            if (!words1.Any() || !words2.Any())
                return 0;

            var intersection = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            var union = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

            return union == 0 ? 0 : (double)intersection / union * 100;
        }

        private double CalculateMedicalTermBonus(string s1, string s2)
        {
            var bonus = 0.0;

            foreach (var mapping in _medicalKeywordMappings)
            {
                var hasS1 = mapping.Value.Any(term => s1.Contains(term, StringComparison.OrdinalIgnoreCase));
                var hasS2 = mapping.Value.Any(term => s2.Contains(term, StringComparison.OrdinalIgnoreCase));

                if (hasS1 && hasS2)
                {
                    bonus += 20.0; // Bonus for matching medical terms
                }
            }

            return Math.Min(bonus, 100.0);
        }

        private double CalculateLengthSimilarity(string s1, string s2)
        {
            if (s1.Length == 0 && s2.Length == 0)
                return 100;

            var maxLength = Math.Max(s1.Length, s2.Length);
            var minLength = Math.Min(s1.Length, s2.Length);

            return (double)minLength / maxLength * 100;
        }

        private List<string> ExtractMedicalKeywords(string text)
        {
            var words = Regex.Split(text.ToLower(), @"\W+")
                .Where(w => w.Length >= 3)
                .Where(w => !IsStopWord(w))
                .Distinct()
                .ToList();

            return words;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>
            {
                "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "from", "up", "about", "into", "through", "during", "before", "after", "above",
                "below", "between", "among", "within", "without", "against", "upon", "beneath",
                "beside", "behind", "beyond", "across", "around", "near", "under", "over"
            };

            return stopWords.Contains(word);
        }

        private List<string> GetMedicalSynonyms(string searchText)
        {
            var synonyms = new List<string>();

            foreach (var mapping in _medicalKeywordMappings)
            {
                if (mapping.Value.Any(term => searchText.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    synonyms.AddRange(mapping.Value);
                }
            }

            return synonyms.Distinct().ToList();
        }

        private string GetConfidenceMessage(double confidence)
        {
            return confidence switch
            {
                >= 90 => "Excellent match - high confidence recommendation",
                >= 80 => "Very good match - recommended for approval",
                >= 70 => "Good match - please review before approval",
                >= 60 => "Moderate match - manual verification recommended",
                >= 40 => "Low confidence match - manual review required",
                _ => "Poor match - manual selection strongly recommended"
            };
        }
    }
}