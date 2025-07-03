using NphiesBridge.Core.Entities;

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
            var response = await _client.GetFromJsonAsync<List<HealthProvider>>("healthproviderapi");
            return response ?? new List<HealthProvider>();
        }

        public async Task AddAsync(HealthProvider provider)
        {
            await _client.PostAsJsonAsync("healthproviderapi", provider);
        }

        public async Task UpdateAsync(Guid id, HealthProvider provider)
        {
            await _client.PutAsJsonAsync($"healthproviderapi/{id}", provider);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"healthproviderapi/{id}");
        }
        public async Task<HealthProvider?> GetByIdAsync(Guid id)
        {
            return await _client.GetFromJsonAsync<HealthProvider>($"healthproviderapi/{id}");
        }
    }
}
