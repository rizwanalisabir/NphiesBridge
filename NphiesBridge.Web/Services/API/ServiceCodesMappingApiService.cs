using System.Net.Http.Headers;
using System.Text.Json;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.Web.Services.API
{
    public class ServiceCodesMappingApiService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ServiceCodesMappingApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _client = httpClientFactory.CreateClient("NphiesAPI"); // BaseAddress points to {apiBaseUrl}/api/
            _httpContextAccessor = httpContextAccessor;
        }

        private void AttachAuth(HttpRequestMessage req)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("ProviderAuthToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<ApiResponse<CreateServiceMappingSessionResponseDto>?> CreateSessionViaExcelAsync(Guid healthProviderId, IFormFile file, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "file", file.FileName);

            var url = $"ServiceCodesMapping/session/upload-excel?healthProviderId={healthProviderId}";
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            AttachAuth(req);

            var res = await _client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ApiResponse<CreateServiceMappingSessionResponseDto>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse<ServiceMappingPageDto>?> GetSessionPageAsync(string sessionId, int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            var url = $"ServiceCodesMapping/session/{sessionId}?page={page}&pageSize={pageSize}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AttachAuth(req);

            var res = await _client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ApiResponse<ServiceMappingPageDto>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse<ServiceMappingStatisticsDto>?> GetStatisticsAsync(string sessionId, CancellationToken ct = default)
        {
            var url = $"ServiceCodesMapping/statistics/{sessionId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AttachAuth(req);

            var res = await _client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ApiResponse<ServiceMappingStatisticsDto>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse?> ApproveAllHighAsync(string sessionId, int max = 100, CancellationToken ct = default)
        {
            var url = $"ServiceCodesMapping/approve-all-high/{sessionId}?max={max}";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            AttachAuth(req);

            var res = await _client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ApiResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse?> SaveMappingAsync(CreateServiceCodeMappingDto dto, CancellationToken ct = default)
        {
            var url = $"ServiceCodesMapping/mappings/save";
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(dto)
            };
            AttachAuth(req);

            var res = await _client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ApiResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}