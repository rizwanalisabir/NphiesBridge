using System.ComponentModel.DataAnnotations.Schema;

namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class IcdCodeMapping : BaseEntity
    {
        public Guid HealthProviderId { get; set; }
        public string NphiesIcdCode { get; set; } = string.Empty;
        public string HealthProviderIcdCode { get; set; } = string.Empty;
        public Guid MappedByUserId { get; set; }
        public DateTime MappedAt { get; set; } = DateTime.UtcNow;
        public bool IsAiSuggested { get; set; } = false;
        public string ConfidenceScore { get; set; }
        public Guid MappingSessionID { get; set; }
    }
}