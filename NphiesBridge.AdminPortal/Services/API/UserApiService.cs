using NphiesBridge.Shared.DTOs;
using System.Net.Http.Headers;

namespace NphiesBridge.AdminPortal.Services.API
{
    public class UserApiService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserApiService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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

        public async Task<List<UserResponseDto>> GetAllAsync()
        {
            SetAuthorizationHeader();
            var apiResponse = await _client.GetFromJsonAsync<ApiResponse<List<UserResponseDto>>>("UserAPI");

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return apiResponse.Data;
            }

            return new List<UserResponseDto>();
        }

        public async Task<UserResponseDto?> GetByIdAsync(Guid id)
        {
            SetAuthorizationHeader();
            var apiResponse = await _client.GetFromJsonAsync<ApiResponse<UserResponseDto>>($"UserAPI/{id}");

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return apiResponse.Data;
            }

            return null;
        }

        public async Task AddAsync(CreateUserDto dto)
        {
            SetAuthorizationHeader();
            var response = await _client.PostAsJsonAsync("UserAPI", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task UpdateAsync(Guid id, UpdateUserDto dto)
        {
            SetAuthorizationHeader();
            var response = await _client.PutAsJsonAsync($"UserAPI/{id}", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            SetAuthorizationHeader();
            var response = await _client.DeleteAsync($"UserAPI/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task ChangePasswordAsync(Guid id, ChangeUserPasswordDto dto)
        {
            SetAuthorizationHeader();
            var response = await _client.PostAsJsonAsync($"UserAPI/{id}/change-password", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }
        }

        // Helper method to get health providers for dropdown
        public async Task<List<HealthProviderResponseDto>> GetHealthProvidersAsync()
        {
            SetAuthorizationHeader();
            var apiResponse = await _client.GetFromJsonAsync<ApiResponse<List<HealthProviderResponseDto>>>("HealthProviderAPI");

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return apiResponse.Data;
            }

            return new List<HealthProviderResponseDto>();
        }
    }
}