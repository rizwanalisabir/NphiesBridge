using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Shared.DTOs
{
    public class CreateSessionRequestDto
    {
        public Guid SessionId { get; set; }
        public Guid HealthProviderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public List<UploadedHospitalCodeDto> HospitalCodes { get; set; } = new List<UploadedHospitalCodeDto>();
    }

    public class CreateSessionResponseDto
    {
        public Guid SessionId { get; set; }
        public int TotalRows { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UploadedHospitalCodeDto
    {
        public string HospitalCode { get; set; } = string.Empty;
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisDescription { get; set; }
        public string? SuggestedIcd10Am { get; set; }
    }
}
