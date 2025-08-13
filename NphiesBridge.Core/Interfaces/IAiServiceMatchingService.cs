using NphiesBridge.Core.Interfaces; // for ServiceResult<T>
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.Core.Interfaces
{
    // Dedicated AI matching service for Service Codes (kept separate from ICD)
    public interface IAiServiceMatchingService
    {
        Task<ServiceResult<ServiceAiSuggestionResponseDto>> GetAiSuggestionAsync(ServiceAiSuggestionRequestDto request, CancellationToken ct = default);

        Task<ServiceResult<List<ServiceAiSuggestionResponseDto>>> GetMultipleSuggestionsAsync(ServiceAiSuggestionRequestDto request, int maxResults = 5, CancellationToken ct = default);

        Task<ServiceResult<BulkServiceMatchingResponseDto>> ProcessBulkMatchingAsync(BulkServiceMatchingRequestDto request, CancellationToken ct = default);
    }
}