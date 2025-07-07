using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Core.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // For provider users - link to their organization
        public Guid? HealthProviderId { get; set; }
        public HealthProvider? HealthProvider { get; set; }
    }

    public class ApplicationRole : IdentityRole<Guid>
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }

        public string? Description { get; set; }
    }
}

// Add this to your existing HealthProvider entity
namespace NphiesBridge.Core.Entities
{
    public partial class HealthProvider
    {
        // Navigation property for users
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}