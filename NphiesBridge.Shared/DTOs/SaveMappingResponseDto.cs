namespace NphiesBridge.Shared.DTOs
{
    public class SaveMappingResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid MappingId { get; set; }
    }
}
