namespace NphiesBridge.Shared.DTOs
{
    public class ExportMappingsRequestDto
    {
        public Guid SessionId { get; set; }
        public bool IncludeUnapproved { get; set; } = true;
    }
}
