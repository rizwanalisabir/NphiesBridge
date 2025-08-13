using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.DTOs
{
    // Excel import rows (Provider side)
    public class ExcelServiceImportDto
    {
        public string? ItemId { get; set; }

        [Required]
        public string ItemRelation { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        // Optional pre-filled target codes
        public string? NphiesCode { get; set; }
        public string? NphiesDescription { get; set; }
    }

    // NPHIES reference item
    public class NphiesServiceCodeDto
    {
        public Guid Id { get; set; }
        public string NphiesServiceCode { get; set; } = string.Empty;
        public string? NphiesServiceDescription { get; set; }
    }

    // Session creation
    public class CreateServiceMappingSessionRequestDto
    {
        [Required]
        public List<ExcelServiceImportDto> Items { get; set; } = new();

        public string OriginalFileName { get; set; } = string.Empty;
    }

    public class CreateServiceMappingSessionResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalRows { get; set; }
    }

    // Page/session fetch for mapping screen
    public class ServiceMappingPageDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public List<ProviderServiceItemDto> ProviderItems { get; set; } = new();
    }

    public class ProviderServiceItemDto
    {
        public Guid Id { get; set; }
        public string? ItemId { get; set; }
        public string ItemRelation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string? NphiesCode { get; set; }
        public string? NphiesDescription { get; set; }

        public bool IsMapped { get; set; }
        public string? SuggestedNphiesCode { get; set; }
        public int ConfidenceScore { get; set; }
        public string? MatchReason { get; set; }
    }

    // Mapping creation
    public class CreateServiceCodeMappingDto
    {
        [Required]
        public Guid ProviderServiceItemId { get; set; }

        [Required]
        public string NphiesServiceCode { get; set; } = string.Empty;

        public bool IsAiSuggested { get; set; } = false;
        public string? ConfidenceScore { get; set; }
    }

    // AI suggestion contracts (separate from ICD)
    public class ServiceAiSuggestionRequestDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? ItemId { get; set; }
        public string? ItemRelation { get; set; }
        public int TopN { get; set; } = 3;
    }

    public class ServiceAiSuggestionResponseDto
    {
        public string SuggestedNphiesCode { get; set; } = string.Empty;
        public string SuggestedDescription { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; } // 0-100
        public string MatchReason { get; set; } = string.Empty;
    }

    // Progress / statistics
    public class ServiceMappingStatisticsDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalCodes { get; set; }
        public int MappedCodes { get; set; }
        public int UnmappedCodes { get; set; }
        public double CompletionPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
    }
}