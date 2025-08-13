using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.Core.Interfaces
{
    public interface IAiFuzzyServiceMappingService
    {
        Task<ServiceAiSuggestionResponseDto> GetAiSuggestionAsync(ServiceAiSuggestionRequestDto request);
        Task<List<ServiceAiMatchSuggestionDto>> BulkSuggestAsync(Guid sessionId, int batchSize = 50);
    }
}