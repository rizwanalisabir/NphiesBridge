using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities.ServiceCodesMapping
{
    public class NphiesServiceCode : BaseEntity
    {
        [Required]
        public string NphiesServiceCodeValue { get; set; } = string.Empty; // unique

        public string? NphiesServiceDescription { get; set; }
    }
}