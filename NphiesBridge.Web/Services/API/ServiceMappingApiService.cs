using NphiesBridge.Shared.DTOs;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NphiesBridge.Web.Services.API
{
    public class ServiceMappingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServiceMappingApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ServiceMappingApiService(HttpClient httpClient, ILogger<ServiceMappingApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        // Create mapping session
        public async Task<ApiResponse<CreateServiceSessionResponseDto>?> CreateMappingSessionAsync(CreateServiceSessionRequestDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/servicemapping/create-session", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<CreateServiceSessionResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to create mapping session. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<CreateServiceSessionResponseDto>.ErrorResult($"Failed to create session: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service mapping session");
                return ApiResponse<CreateServiceSessionResponseDto>.ErrorResult("Failed to create mapping session");
            }
        }

        // Get mapping session details
        public async Task<ApiResponse<ServiceMappingPageDto>?> GetMappingSessionAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/servicemapping/session/{sessionId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<ServiceMappingPageDto>>(content, _jsonOptions);
                }

                _logger.LogWarning("Failed to get mapping session. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<ServiceMappingPageDto>.ErrorResult($"Failed to get session: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service mapping session: {SessionId}", sessionId);
                return ApiResponse<ServiceMappingPageDto>.ErrorResult("Failed to retrieve mapping session");
            }
        }

        // Save a mapping
        public async Task<ApiResponse<SuccessResponse>?> SaveMappingAsync(SaveServiceMappingRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/servicemapping/save-mapping", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<SuccessResponse>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to save mapping. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<SuccessResponse>.ErrorResult($"Failed to save mapping: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service mapping");
                return ApiResponse<SuccessResponse>.ErrorResult("Failed to save mapping");
            }
        }

        // Get AI suggestion
        public async Task<ApiResponse<ServiceAiSuggestionResponseDto>?> GetAiSuggestionAsync(ServiceAiSuggestionRequestDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/servicemapping/ai-suggestion", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<ServiceAiSuggestionResponseDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to get AI suggestion. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult($"Failed to get AI suggestion: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI suggestion for service mapping");
                return ApiResponse<ServiceAiSuggestionResponseDto>.ErrorResult("Failed to get AI suggestion");
            }
        }

        // Bulk match
        public async Task<ApiResponse<BulkServiceMatchingResponse>?> BulkMatchAsync(BulkServiceMatchingRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/servicemapping/bulk-match", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<BulkServiceMatchingResponse>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to bulk match. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<BulkServiceMatchingResponse>.ErrorResult($"Failed to bulk match: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk matching service mapping");
                return ApiResponse<BulkServiceMatchingResponse>.ErrorResult("Failed to bulk match");
            }
        }

        // Get statistics
        public async Task<ApiResponse<ServiceMappingStatisticsDto>?> GetStatisticsAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/servicemapping/statistics/{sessionId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiResponse<ServiceMappingStatisticsDto>>(responseContent, _jsonOptions);
                }

                _logger.LogWarning("Failed to get statistics. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<ServiceMappingStatisticsDto>.ErrorResult($"Failed to get statistics: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for service mapping");
                return ApiResponse<ServiceMappingStatisticsDto>.ErrorResult("Failed to get statistics");
            }
        }

        // Export mappings
        public async Task<byte[]?> ExportMappingsAsync(ExportServiceMappingsRequest request)
        {
            try
            {
                var queryParams = $"?sessionId={request.SessionId}&includeUnmapped={request.IncludeUnmapped}&format={request.Format}";
                var response = await _httpClient.GetAsync($"api/servicemapping/export{queryParams}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                _logger.LogWarning("Failed to export mappings. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting service mappings");
                return null;
            }
        }
    }
}