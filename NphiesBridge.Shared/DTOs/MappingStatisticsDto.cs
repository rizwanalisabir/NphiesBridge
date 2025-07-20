namespace NphiesBridge.Shared.DTOs
{
    public class MappingStatisticsDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalCodes { get; set; }
        public int MappedCodes { get; set; }
        public int UnmappedCodes { get; set; }
        public double CompletionPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string HealthProviderName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;

        // Additional useful statistics
        public int HighConfidenceMatches { get; set; }
        public int MediumConfidenceMatches { get; set; }
        public int LowConfidenceMatches { get; set; }
        public int ManualMappings { get; set; }
        public int AiSuggestedMappings { get; set; }

        // Processing time statistics
        public DateTime CreatedAt { get; set; }
        public TimeSpan? ProcessingDuration => CompletedAt.HasValue
            ? CompletedAt.Value - CreatedAt
            : DateTime.UtcNow - CreatedAt;
    }
}