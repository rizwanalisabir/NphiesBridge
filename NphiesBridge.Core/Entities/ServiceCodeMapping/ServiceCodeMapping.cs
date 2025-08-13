using System.ComponentModel.DataAnnotations.Schema;

namespace NphiesBridge.Core.Entities.ServiceMapping
{
    public class ServiceCodeMapping : BaseEntity
    {
        public Guid HealthProviderId { get; set; }
        public string NphiesServiceCode { get; set; } = string.Empty;
        public string HealthProviderServiceId { get; set; } = string.Empty;
        public string HealthProviderServiceRelation { get; set; } = string.Empty;
        public Guid MappedByUserId { get; set; }
        public DateTime MappedAt { get; set; } = DateTime.UtcNow;
        public bool IsAiSuggested { get; set; } = false;
        public string ConfidenceScore { get; set; }
        public Guid ServiceMappingSessionID { get; set; }
    }
}