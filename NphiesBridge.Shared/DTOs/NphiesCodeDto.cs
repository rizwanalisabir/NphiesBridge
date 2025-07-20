namespace NphiesBridge.Shared.DTOs
{
    public class NphiesCodeDto
    {
        public string Id { get; set; } = string.Empty; // This is the Code
        public string Text { get; set; } = string.Empty; // Code + Description for Select2
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
    }
}