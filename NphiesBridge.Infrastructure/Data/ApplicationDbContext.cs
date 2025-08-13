using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Entities.IcdMapping;
using NphiesBridge.Core.Entities.ServiceCodesMapping;

namespace NphiesBridge.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<HealthProvider> HealthProviders { get; set; }
        // ICD Mapping Tables
        public DbSet<HospitalIcdCode> HospitalIcdCodes { get; set; }
        public DbSet<NphiesIcdCode> NphiesIcdCodes { get; set; }
        public DbSet<IcdCodeMapping> IcdCodeMappings { get; set; }
        public DbSet<MappingSession> MappingSessions { get; set; }

        public DbSet<NphiesServiceCode> NphiesServiceCodes { get; set; } = null!;
        public DbSet<ServiceCodesMappingSession> ServiceCodesMappingSessions { get; set; } = null!;
        public DbSet<ProviderServiceItem> ProviderServiceItems { get; set; } = null!;
        public DbSet<ServiceCodeMapping> ServiceCodeMappings { get; set; } = null!;

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
            ConfigureServiceCodesMapping(modelBuilder);
        }

        private static void ConfigureServiceCodesMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NphiesServiceCode>(e =>
            {
                e.ToTable("NphiesServiceCodes");
                e.HasIndex(x => x.NphiesServiceCodeValue).IsUnique();
                e.Property(x => x.NphiesServiceCodeValue).IsRequired();
            });

            modelBuilder.Entity<ServiceCodesMappingSession>(e =>
            {
                e.ToTable("ServiceCodesMappingSessions");
                e.HasIndex(x => x.SessionId).IsUnique();
                e.Property(x => x.SessionId).HasMaxLength(100).IsRequired();
                e.HasMany(x => x.ProviderItems)
                    .WithOne(x => x.Session)
                    .HasForeignKey(x => x.ServiceCodesMappingSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProviderServiceItem>(e =>
            {
                e.ToTable("ProviderServiceItems");
                e.Property(x => x.ItemRelation).IsRequired().HasMaxLength(128);
                e.Property(x => x.Name).IsRequired().HasMaxLength(512);
                e.HasIndex(x => new { x.ServiceCodesMappingSessionId, x.ItemRelation }).IsUnique();
            });

            modelBuilder.Entity<ServiceCodeMapping>(e =>
            {
                e.ToTable("ServiceCodeMappings");
                e.Property(x => x.ProviderItemRelation).IsRequired().HasMaxLength(128);
                e.Property(x => x.NphiesServiceCodeValue).IsRequired().HasMaxLength(128);

                // Ensure only one mapping per provider per ItemRelation
                e.HasIndex(x => new { x.HealthProviderId, x.ProviderItemRelation }).IsUnique();
            });
        }
    }
}