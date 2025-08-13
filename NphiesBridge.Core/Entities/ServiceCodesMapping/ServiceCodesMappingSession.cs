namespace NphiesBridge.Core.Entities.ServiceCodesMapping
{
    public class ServiceCodesMappingSession : BaseEntity
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid HealthProviderId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; } = 0;
        public string Status { get; set; } = "Processing";
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public ICollection<ProviderServiceItem> ProviderItems { get; set; } = new List<ProviderServiceItem>();
    }
}