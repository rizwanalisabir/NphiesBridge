using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities.ServiceCodesMapping
{
    public class ServiceCodeMapping : BaseEntity
    {
        public Guid HealthProviderId { get; set; }

        // Provider identifiers in mapping (ItemRelation required, ItemId optional)
        [Required]
        public string ProviderItemRelation { get; set; } = string.Empty;

        public string? ProviderItemId { get; set; }

        public string? ProviderItemName { get; set; }

        // Target NPHIES code
        [Required]
        public string NphiesServiceCodeValue { get; set; } = string.Empty;

        public Guid MappedByUserId { get; set; }
        public DateTime MappedAt { get; set; } = DateTime.UtcNow;

        public bool IsAiSuggested { get; set; } = false;
        public string? ConfidenceScore { get; set; } // string to mirror ICD style
    }
}