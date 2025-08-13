using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.DTOs
{
    // Bulk AI matching request/response DTOs for Service Codes

    public class BulkServiceMatchingRequestDto
    {
        [Required]
        public List<BulkServiceMatchingItemDto> Items { get; set; } = new();
        public int TopN { get; set; } = 3;
    }

    public class BulkServiceMatchingItemDto
    {
        public Guid? ProviderServiceItemId { get; set; } // optional: to map back to row
        public string? ItemId { get; set; }
        public string? ItemRelation { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class BulkServiceMatchingResponseDto
    {
        public List<BulkServiceMatchingResultItemDto> Results { get; set; } = new();
        public int TotalProcessed { get; set; }
    }

    public class BulkServiceMatchingResultItemDto
    {
        public Guid? ProviderServiceItemId { get; set; }
        public string InputName { get; set; } = string.Empty;
        public string? InputItemId { get; set; }
        public string? InputItemRelation { get; set; }

        public List<ServiceAiSuggestionResponseDto> Suggestions { get; set; } = new();
    }
}