namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class ServiceMappingSession : BaseEntity
    {
        public Guid SessionId { get; set; }
        public Guid HealthProviderId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; } = 0;
        public string Status { get; set; } = "Processing";
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        public HealthProvider HealthProvider { get; set; } = null!;
        public ICollection<HospitalServiceCode> HospitalCodes { get; set; } = new List<HospitalServiceCode>();
    }
}