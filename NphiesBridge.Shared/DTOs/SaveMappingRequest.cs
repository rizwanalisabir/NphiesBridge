using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Shared.DTOs
{
    using System.ComponentModel.DataAnnotations;

    namespace NphiesBridge.Shared.DTOs
    {
        public class SaveMappingRequest
        {
            public Guid HospitalCodeId { get; set; }

            [Required]
            [StringLength(20)]
            public string HospitalIcdCode { get; set; } = string.Empty;

            [Required]
            [StringLength(20)]
            public string NphiesIcdCode { get; set; } = string.Empty;

            public bool IsAiSuggested { get; set; }

            [Range(0, 100)]
            public string ConfidenceScore { get; set; }
        }

        public class SaveBulkMappingsRequest
        {
            [Required]
            public List<SaveMappingRequest> Mappings { get; set; } = new();
        }

        public class MappingStatusResponse
        {
            public Guid HospitalCodeId { get; set; }
            public bool IsMapped { get; set; }
            public string? NphiesIcdCode { get; set; }
            public string ConfidenceScore { get; set; }
        }
    }
}
