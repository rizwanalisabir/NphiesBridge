using NphiesBridge.Core.Entities.IcdMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Core.Entities.ServiceMapping
{
    public class HealthProviderServiceCode : BaseEntity
    {
        public Guid HealthProviderId { get; set; }
        public string HealthProviderServiceId { get; set; } = string.Empty;
        public string HealthProviderServiceRelation { get; set; } = string.Empty;
        public string HealthProviderServiceName { get; set; } = string.Empty;
        public string? NphiesServiceCode { get; set; } // From Excel template
        public bool IsMapped { get; set; } = false;
        public Guid? ServiceMappingSessionId { get; set; }
        public ServiceMappingSession? ServiceMappingSession { get; set; } = null!;

        // Navigation
        public HealthProvider HealthProvider { get; set; } = null!;
        public ICollection<ServiceCodeMapping> Mappings { get; set; } = new List<ServiceCodeMapping>();
    }
}