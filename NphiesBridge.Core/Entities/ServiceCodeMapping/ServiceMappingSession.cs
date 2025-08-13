namespace NphiesBridge.Core.Entities.ServiceMapping
{
    public class ServiceMappingSession : BaseEntity
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid HealthProviderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int CompletedRows { get; set; }
        public string Status { get; set; } = "Created"; // Created, Processing, Completed, Failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public HealthProvider HealthProvider { get; set; } = null!;
        public ICollection<HealthProviderServiceCode> HealthProviderServiceCodes { get; set; } = new List<HealthProviderServiceCode>();
    }
}