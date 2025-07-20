namespace NphiesBridge.Shared.DTOs
{
    public class BulkMatchingRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public List<HospitalCodeDto> HospitalCodes { get; set; } = new List<HospitalCodeDto>();
    }
}
