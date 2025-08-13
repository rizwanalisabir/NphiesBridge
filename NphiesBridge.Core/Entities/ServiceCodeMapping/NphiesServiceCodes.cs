using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities.ServiceCodeMapping
{
    public class NphiesServiceCodes : BaseEntity
    {
        [Required]
        public string NphiesServiceCodeValue { get; set; } = string.Empty; // unique

        public string? NphiesServiceDescription { get; set; }
    }
}