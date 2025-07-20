
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Shared.DTOs
{
    public class AiSuggestionRequestDto
    {
        public Guid HospitalCodeId { get; set; }
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisDescription { get; set; }
        public string? SuggestedIcd10Am { get; set; }
        public string HospitalCode { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}
