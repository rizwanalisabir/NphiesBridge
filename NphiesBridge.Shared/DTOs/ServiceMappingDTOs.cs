using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.DTOs
{
    // For Excel Import
    public class ExcelServiceImportDto
    {
        public string? HealthProviderServiceId { get; set; }

        [Required]
        public string HealthProviderServiceRelation { get; set; } = string.Empty;

        [Required]
        public string HealthProviderServiceName { get; set; } = string.Empty;

        public string? NphiesServiceCode { get; set; }
    }

    // For displaying health provider service codes
    public class HealthProviderServiceCodeDto
    {
        public Guid Id { get; set; }
        public string HealthProviderServiceId { get; set; } = string.Empty;
        public string HealthProviderServiceRelation { get; set; } = string.Empty;
        public string HealthProviderServiceName { get; set; } = string.Empty;
        public string? NphiesServiceCode { get; set; }
        public bool IsMapped { get; set; }
        public string? MappedToCode { get; set; }
        public int MatchConfidence { get; set; } // 0-100%
    }

    // For NPHIES service codes
    public class NphiesServiceCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
    }

    // For creating mappings
    public class CreateServiceMappingDto
    {
        [Required]
        public Guid HealthProviderServiceCodeId { get; set; }

        [Required]
        public string NphiesServiceCode { get; set; } = string.Empty;
    }

    // For AI matching results
    public class ServiceAiMatchSuggestionDto
    {
        public Guid HealthProviderServiceCodeId { get; set; }
        public string NphiesServiceCode { get; set; } = string.Empty;
        public string NphiesDescription { get; set; } = string.Empty;
        public string ConfidenceScore { get; set; } // 0-100
        public string MatchReason { get; set; } = string.Empty; // "Code match", "Description similarity", etc.
    }

    // For import progress tracking
    public class ServiceImportProgressDto
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // For mapping progress
    public class ServiceMappingProgressDto
    {
        public int TotalCodes { get; set; }
        public int MappedCodes { get; set; }
        public int UnmappedCodes { get; set; }
        public int AiSuggestedCodes { get; set; }
        public decimal CompletionPercentage { get; set; }
    }
}