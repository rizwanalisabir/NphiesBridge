using NphiesBridge.Shared.DTOs;
using System.Text.Json;
using System.Text;

namespace NphiesBridge.Web.Services.API
{
    public class IcdMappingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IcdMappingApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public IcdMappingApiService(HttpClient httpClient, ILogger<IcdMappingApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Create mapping session via API
        /// </summary>
        public async Task<ApiResponse<CreateSessionResponseDto>?> CreateMappingSessionAsync(CreateSessionRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating mapping session for {Count} hospital codes", request.HospitalCodes.Count);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/icdmapping/create-session", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<CreateSessionResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to create mapping session. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<CreateSessionResponseDto>.ErrorResult($"Failed to create session: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mapping session");
                return ApiResponse<CreateSessionResponseDto>.ErrorResult("Failed to create mapping session");
            }
        }

        public async Task<ApiResponse<IcdMappingPageDto>?> GetMappingSessionAsync(string sessionId)
        {
            try
            {
                _logger.LogInformation("Getting mapping session: {SessionId}", sessionId);

                var response = await _httpClient.GetAsync($"api/icdmapping/session/{sessionId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<IcdMappingPageDto>>(content, _jsonOptions);
                }

                _logger.LogWarning("Failed to get mapping session. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<IcdMappingPageDto>.ErrorResult($"Failed to get session: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapping session: {SessionId}", sessionId);
                return ApiResponse<IcdMappingPageDto>.ErrorResult("Failed to retrieve mapping session");
            }
        }

        public async Task<ApiResponse<AiSuggestionResponseDto>?> GetAiSuggestionAsync(AiSuggestionRequestDto request)
        {
            try
            {
                _logger.LogInformation("Getting AI suggestion for hospital code: {HospitalCode}", request.HospitalCode);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/icdmapping/ai-suggestion", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<AiSuggestionResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to get AI suggestion. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<AiSuggestionResponseDto>.ErrorResult($"Failed to get AI suggestion: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI suggestion for hospital code: {HospitalCode}", request.HospitalCode);
                return ApiResponse<AiSuggestionResponseDto>.ErrorResult("Failed to get AI suggestion");
            }
        }

        public async Task<ApiResponse<SaveMappingResponseDto>?> SaveMappingAsync(SaveMappingRequestDto request)
        {
            try
            {
                _logger.LogInformation("Saving mapping for hospital code ID: {HospitalCodeId}", request.HospitalCodeId);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/icdmapping/save-mapping", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<SaveMappingResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to save mapping. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<SaveMappingResponseDto>.ErrorResult($"Failed to save mapping: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving mapping for hospital code ID: {HospitalCodeId}", request.HospitalCodeId);
                return ApiResponse<SaveMappingResponseDto>.ErrorResult("Failed to save mapping");
            }
        }

        public async Task<ApiResponse<MappingStatisticsDto>?> GetMappingStatisticsAsync(string sessionId)
        {
            try
            {
                _logger.LogInformation("Getting mapping statistics for session: {SessionId}", sessionId);

                var response = await _httpClient.GetAsync($"api/icdmapping/statistics/{sessionId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<MappingStatisticsDto>>(content, _jsonOptions);
                }

                _logger.LogWarning("Failed to get mapping statistics. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<MappingStatisticsDto>.ErrorResult($"Failed to get statistics: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapping statistics for session: {SessionId}", sessionId);
                return ApiResponse<MappingStatisticsDto>.ErrorResult("Failed to retrieve statistics");
            }
        }

        public async Task<ApiResponse<BulkMatchingResponseDto>?> ProcessBulkMatchingAsync(BulkMatchingRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing bulk matching for session: {SessionId}", request.SessionId);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/icdmapping/bulk-match", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<BulkMatchingResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to process bulk matching. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<BulkMatchingResponseDto>.ErrorResult($"Failed to process bulk matching: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk matching for session: {SessionId}", request.SessionId);
                return ApiResponse<BulkMatchingResponseDto>.ErrorResult("Failed to process bulk matching");
            }
        }

        public async Task<byte[]?> ExportMappingsAsync(ExportMappingsRequestDto request)
        {
            try
            {
                _logger.LogInformation("Exporting mappings for session: {SessionId}", request.SessionId);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/icdmapping/export", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                _logger.LogWarning("Failed to export mappings. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting mappings for session: {SessionId}", request.SessionId);
                return null;
            }
        }
    }

    // Extension method for HttpClient registration
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIcdMappingApiService(this IServiceCollection services, string apiBaseUrl)
        {
            services.AddHttpClient<IcdMappingApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromMinutes(5); // For potentially long AI processing
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            return services;
        }
    }

}