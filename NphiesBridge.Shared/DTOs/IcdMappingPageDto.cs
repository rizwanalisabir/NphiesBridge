namespace NphiesBridge.Shared.DTOs
{
    public class IcdMappingPageDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public List<HospitalCodeDto> HospitalCodes { get; set; } = new List<HospitalCodeDto>();
    }
}
