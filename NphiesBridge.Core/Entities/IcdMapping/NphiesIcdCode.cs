namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class NphiesServiceCode : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Chapter { get; set; }
        public bool IsActive { get; set; } = true;
    }
}