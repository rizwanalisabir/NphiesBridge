using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Entities.IcdMapping;

namespace NphiesBridge.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<HealthProvider> HealthProviders { get; set; }
        // ICD Mapping Tables
        public DbSet<HospitalServiceCode> HospitalIcdCodes { get; set; }
        public DbSet<NphiesServiceCode> NphiesIcdCodes { get; set; }
        public DbSet<ServiceCodeMapping> IcdCodeMappings { get; set; }
        public DbSet<ServiceMappingSession> MappingSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HealthProvider configurations
            modelBuilder.Entity<HealthProvider>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique(false);

            // User-HealthProvider relationship
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.HealthProvider)
                .WithMany(h => h.Users)
                .HasForeignKey(u => u.HealthProviderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed default roles
            var adminRoleId = Guid.NewGuid();
            var providerRoleId = Guid.NewGuid();

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "System Administrator"
                },
                new ApplicationRole
                {
                    Id = providerRoleId,
                    Name = "Provider",
                    NormalizedName = "PROVIDER",
                    Description = "Healthcare Provider User"
                }
            );
        }
    }
}