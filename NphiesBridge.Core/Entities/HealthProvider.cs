using System;
using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities
{
    public class HealthProvider
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string LicenseNumber { get; set; }

        [MaxLength(200)]
        public string ContactPerson { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
