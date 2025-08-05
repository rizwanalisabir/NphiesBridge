namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class IcdCodeMapping : BaseEntity
    {
        public Guid HospitalCodeId { get; set; }
        public string NphiesIcdCode { get; set; } = string.Empty;
        public Guid MappedByUserId { get; set; }
        public DateTime MappedAt { get; set; } = DateTime.UtcNow;
        public bool IsAiSuggested { get; set; } = false;
        public string ConfidenceScore { get; set; } // For AI suggestions

        // Navigation
        public HospitalIcdCode HospitalCode { get; set; } = null!;
        public ApplicationUser MappedByUser { get; set; } = null!;
    }
}