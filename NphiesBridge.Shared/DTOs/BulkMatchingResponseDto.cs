namespace NphiesBridge.Shared.DTOs
{
    public class BulkMatchingResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}