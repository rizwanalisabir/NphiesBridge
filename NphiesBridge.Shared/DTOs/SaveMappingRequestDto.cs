namespace NphiesBridge.Shared.DTOs
{
    public class SaveMappingRequestDto
    {
        public Guid HospitalCodeId { get; set; }
        public string NphiesCode { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public int RowNumber { get; set; }
    }
}
