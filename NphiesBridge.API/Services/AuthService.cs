using NphiesBridge.Shared.DTOs;
using System.Text.Json;

namespace NphiesBridge.AdminPortal.Services.API
{
    public class AuthService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _client = httpClientFactory.CreateClient("NphiesAPI");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("Auth/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResponseDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Store token in session
                        StoreToken(apiResponse.Data.Token);
                        StoreUserInfo(apiResponse.Data.User);
                        return apiResponse.Data;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                // Call API logout endpoint
                await _client.PostAsync("Auth/logout", null);

                // Clear local storage
                ClearToken();
                ClearUserInfo();

                return true;
            }
            catch
            {
                // Even if API call fails, clear local storage
                ClearToken();
                ClearUserInfo();
                return false;
            }
        }

        public string? GetToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }

        public UserInfoDto? GetCurrentUser()
        {
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString("CurrentUser");
            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<UserInfoDto>(userJson);
            }
            catch
            {
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            var token = GetToken();
            var user = GetCurrentUser();
            return !string.IsNullOrEmpty(token) && user != null;
        }

        public bool IsInRole(string role)
        {
            var user = GetCurrentUser();
            return user?.Roles?.Contains(role) == true;
        }

        private void StoreToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("AuthToken", token);
        }

        private void StoreUserInfo(UserInfoDto user)
        {
            var userJson = JsonSerializer.Serialize(user);
            _httpContextAccessor.HttpContext?.Session.SetString("CurrentUser", userJson);
        }

        private void ClearToken()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("AuthToken");
        }

        private void ClearUserInfo()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("CurrentUser");
        }
    }
}