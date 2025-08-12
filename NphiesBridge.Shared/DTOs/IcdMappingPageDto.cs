namespace NphiesBridge.Shared.DTOs
{
    public class IcdMappingPageDto
    {
        public Guid SessionId { get; set; }
        public int TotalRows { get; set; }
        public List<HospitalCodeDto> HospitalCodes { get; set; } = new List<HospitalCodeDto>();
    }
}
