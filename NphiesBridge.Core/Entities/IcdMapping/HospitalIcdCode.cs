using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Core.Entities.IcdMapping
{
    public class HospitalServiceCode : BaseEntity
    {
        public Guid HealthProviderId { get; set; }
        public string HospitalCode { get; set; } = string.Empty;
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisDescription { get; set; }
        public string? SuggestedIcd10Am { get; set; } // From Excel template
        public bool IsMapped { get; set; } = false;
        public Guid? MappingSessionId { get; set; } // Add this line
        public ServiceMappingSession? MappingSession { get; set; } = null!; // Add this line

        // Navigation
        public HealthProvider HealthProvider { get; set; } = null!;
        public ICollection<ServiceCodeMapping> Mappings { get; set; } = new List<ServiceCodeMapping>();
    }
}
