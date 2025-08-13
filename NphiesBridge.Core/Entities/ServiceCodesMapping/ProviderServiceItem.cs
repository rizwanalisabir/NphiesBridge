using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities.ServiceCodesMapping
{
    public class ProviderServiceItem : BaseEntity
    {
        public Guid ServiceCodesMappingSessionId { get; set; }
        public ServiceCodesMappingSession Session { get; set; } = null!;

        public Guid HealthProviderId { get; set; }

        // Provider side identifiers (at least one of ItemRelation or ItemId should exist)
        public string? ItemId { get; set; }

        [Required]
        public string ItemRelation { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        // Optional pre-filled NPHIES data from import (not authoritative)
        public string? NphiesCode { get; set; }
        public string? NphiesDescription { get; set; }

        // Processing state
        public bool IsMapped { get; set; } = false;
        public string? SuggestedNphiesCode { get; set; }
        public int ConfidenceScore { get; set; } = 0; // 0-100
        public string? MatchReason { get; set; }
    }
}