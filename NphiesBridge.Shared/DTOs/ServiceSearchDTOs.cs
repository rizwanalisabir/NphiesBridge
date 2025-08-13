namespace NphiesBridge.Shared.DTOs
{
    // For Select2 dropdown search
    public class NphiesServiceDropdownDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
    }

    // For search results
    public class ServiceSearchResultDto
    {
        public string NphiesServiceCode { get; set; } = string.Empty;
        public string NphiesServiceDescription { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
        public bool IsActive { get; set; }
    }

    // For search request
    public class ServiceSearchRequestDto
    {
        public string Query { get; set; } = string.Empty;
        public int Limit { get; set; } = 50;
        public bool ActiveOnly { get; set; } = true;
    }
}