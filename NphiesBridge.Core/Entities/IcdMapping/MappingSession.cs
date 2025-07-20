namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class MappingSession : BaseEntity
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid HealthProviderId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; } = 0;
        public string Status { get; set; } = "Processing";
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        public HealthProvider HealthProvider { get; set; } = null!;
        public ICollection<HospitalIcdCode> HospitalCodes { get; set; } = new List<HospitalIcdCode>();
    }
}