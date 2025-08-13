using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.DTOs
{
    // Create Session Request/Response
    public class CreateServiceSessionRequestDto
    {
        [Required]
        public Guid HealthProviderId { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public List<ExcelServiceImportDto> HealthProviderServiceCodes { get; set; } = new();
    }

    public class CreateServiceSessionResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Service Mapping Page DTO
    public class ServiceMappingPageDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public List<HealthProviderServiceCodeDto> HealthProviderServiceCodes { get; set; } = new();
    }

    // AI Suggestion Request/Response
    public class ServiceAiSuggestionRequestDto
    {
        [Required]
        public string HealthProviderServiceId { get; set; } = string.Empty;

        [Required]
        public string HealthProviderServiceRelation { get; set; } = string.Empty;

        [Required]
        public string HealthProviderServiceName { get; set; } = string.Empty;
    }

    public class ServiceAiSuggestionResponseDto
    {
        public bool Success { get; set; }
        public NphiesServiceCodeDto? SuggestedCode { get; set; }
        public int Confidence { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Save Mapping Request
    public class SaveServiceMappingRequest
    {
        [Required]
        public Guid HealthProviderId { get; set; }

        [Required]
        public string HealthProviderServiceId { get; set; } = string.Empty;

        [Required]
        public string HealthProviderServiceRelation { get; set; } = string.Empty;

        [Required]
        public string NphiesServiceCode { get; set; } = string.Empty;

        [Required]
        public Guid MappedBy { get; set; }

        public bool IsAiSuggested { get; set; }
        public int ConfidenceScore { get; set; }
    }

    // Bulk Matching Request/Response
    public class BulkServiceMatchingRequest
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        public int BatchSize { get; set; } = 10;
    }

    public class BulkServiceMatchingResponse
    {
        public bool Success { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    // Export Request
    public class ExportServiceMappingsRequest
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        public bool IncludeUnmapped { get; set; } = true;
        public string Format { get; set; } = "Excel"; // Excel, CSV
    }

    // Statistics DTO
    public class ServiceMappingStatisticsDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int MappedServices { get; set; }
        public int UnmappedServices { get; set; }
        public int AiSuggestedServices { get; set; }
        public int ManuallyMappedServices { get; set; }
        public decimal CompletionPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}