using NphiesBridge.Core.Entities;
using NphiesBridge.Shared.DTOs;
using System.Net.Http.Headers;

namespace NphiesBridge.AdminPortal.Services.API
{
    public class HealthProviderApiService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HealthProviderApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _client = httpClientFactory.CreateClient("NphiesAPI");
            _httpContextAccessor = httpContextAccessor;
        }

        private void SetAuthorizationHeader()
        {
            var authService = _httpContextAccessor.HttpContext?.RequestServices.GetService<AuthService>();
            var token = authService?.GetToken();

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<HealthProvider>> GetAllAsync()
        {
            SetAuthorizationHeader();
            var apiResponse = await _client.GetFromJsonAsync<ApiResponse<List<HealthProviderResponseDto>>>("healthprovider");

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                // Convert DTOs back to entities
                return apiResponse.Data.Select(dto => new HealthProvider
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    LicenseNumber = dto.LicenseNumber,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive,
                    CreatedAt = dto.CreatedAt
                }).ToList();
            }

            return new List<HealthProvider>();
        }

        public async Task AddAsync(HealthProvider provider)
        {
            SetAuthorizationHeader();
            var response = await _client.PostAsJsonAsync("healthprovider", provider);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task UpdateAsync(Guid id, HealthProvider provider)
        {
            SetAuthorizationHeader();
            await _client.PutAsJsonAsync($"healthprovider/{id}", provider);
        }

        public async Task DeleteAsync(Guid id)
        {
            SetAuthorizationHeader();
            await _client.DeleteAsync($"healthprovider/{id}");
        }

        public async Task<HealthProvider?> GetByIdAsync(Guid id)
        {
            SetAuthorizationHeader();
            var apiResponse = await _client.GetFromJsonAsync<ApiResponse<HealthProviderResponseDto>>($"healthprovider/{id}");

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                var dto = apiResponse.Data;
                return new HealthProvider
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    LicenseNumber = dto.LicenseNumber,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive,
                    CreatedAt = dto.CreatedAt
                };
            }

            return null;
        }
    }
}