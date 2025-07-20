using NphiesBridge.Shared.DTOs;
using NphiesBridge.Shared.Models;

namespace NphiesBridge.Core.Interfaces
{
    public interface IAiFuzzyMatchingService
    {
        Task<ServiceResult<AiSuggestionResponseDto>> GetAiSuggestionAsync(AiSuggestionRequestDto request);
        Task<ServiceResult<List<FuzzyMatchResult>>> GetMultipleSuggestionsAsync(AiSuggestionRequestDto request, int maxResults = 5);
        Task<ServiceResult<BulkMatchingResponseDto>> ProcessBulkMatchingAsync(BulkMatchingRequestDto request);
    }

    // Service Result wrapper for business logic
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static ServiceResult<T> Failure(string errorMessage, List<string>? validationErrors = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ValidationErrors = validationErrors ?? new List<string>()
            };
        }
    }
}