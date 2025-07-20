namespace NphiesBridge.Shared.DTOs
{
    public class ExportMappingsRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public bool IncludeUnapproved { get; set; } = true;
    }
}
