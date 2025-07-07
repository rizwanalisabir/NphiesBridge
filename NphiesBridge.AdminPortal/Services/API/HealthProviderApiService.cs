using NphiesBridge.Core.Entities;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.AdminPortal.Services.API
{
    public class HealthProviderApiService
    {
        private readonly HttpClient _client;

        public HealthProviderApiService(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("NphiesAPI");
        }

        public async Task<List<HealthProvider>> GetAllAsync()
        {
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
            var response = await _client.PostAsJsonAsync("healthprovider", provider);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task UpdateAsync(Guid id, HealthProvider provider)
        {
            await _client.PutAsJsonAsync($"healthprovider/{id}", provider);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"healthprovider/{id}");
        }
        public async Task<HealthProvider?> GetByIdAsync(Guid id)
        {
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
