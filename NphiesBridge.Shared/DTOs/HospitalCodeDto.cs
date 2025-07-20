namespace NphiesBridge.Shared.DTOs
{
    public class HospitalCodeDto
    {
        public Guid Id { get; set; }
        public string HospitalCode { get; set; } = string.Empty;
        public string DiagnosisName { get; set; } = string.Empty;
        public string? DiagnosisDescription { get; set; }
        public string? SuggestedIcd10Am { get; set; }
        public bool IsMapped { get; set; }
    }
}
