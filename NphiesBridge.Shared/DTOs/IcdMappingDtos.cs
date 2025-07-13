using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.DTOs
{
    // For Excel Import
    public class ExcelIcdImportDto
    {
        public string? Icd10AmCode { get; set; }

        [Required]
        public string HospitalCode { get; set; } = string.Empty;

        [Required]
        public string DiagnosisName { get; set; } = string.Empty;

        public string? DiagnosisDescription { get; set; }
    }

    // For displaying hospital codes
    public class HospitalIcdCodeDto
    {
        public Guid Id { get; set; }
        public string HospitalCode { get; set; } = string.Empty;
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisDescription { get; set; }
        public string? SuggestedIcd10Am { get; set; }
        public bool IsMapped { get; set; }
        public string? MappedToCode { get; set; }
        public int MatchConfidence { get; set; } // 0-100%
    }

    // For NPHIES standard codes
    public class NphiesIcdCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
    }

    // For creating mappings
    public class CreateIcdMappingDto
    {
        [Required]
        public Guid HospitalCodeId { get; set; }

        [Required]
        public string NphiesIcdCode { get; set; } = string.Empty;
    }

    // For AI matching results
    public class AiMatchSuggestionDto
    {
        public Guid HospitalCodeId { get; set; }
        public string NphiesIcdCode { get; set; } = string.Empty;
        public string NphiesDescription { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; } // 0-100
        public string MatchReason { get; set; } = string.Empty; // "Code match", "Description similarity", etc.
    }

    // For import progress tracking
    public class ImportProgressDto
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
    public class MappingProgressDto
    {
        public int TotalCodes { get; set; }
        public int MappedCodes { get; set; }
        public int UnmappedCodes { get; set; }
        public int AiSuggestedCodes { get; set; }
        public decimal CompletionPercentage { get; set; }
    }
}