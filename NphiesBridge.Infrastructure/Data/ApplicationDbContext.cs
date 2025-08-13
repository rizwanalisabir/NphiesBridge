using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Entities.IcdMapping;
using NphiesBridge.Core.Entities.ServiceCodeMapping;
using NphiesBridge.Core.Entities.ServiceMapping;

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
        public DbSet<ServiceCodeMapping> ServiceCodeMappings { get; set; } = null!;
        public DbSet<HealthProviderServiceCode> HealthProviderServiceCodes { get; set; } = null!;
        public DbSet<ServiceMappingSession> ServiceMappingSessions { get; set; } = null!;

        public DbSet<NphiesServiceCodes> NphiesServiceCodes { get; set; } = null!;


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

            modelBuilder.Entity<ServiceCodeMapping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NphiesServiceCode).HasMaxLength(128).IsRequired();
                entity.Property(e => e.HealthProviderServiceRelation).HasMaxLength(128).IsRequired();
                entity.Property(e => e.HealthProviderServiceId).HasMaxLength(128);
                entity.Property(e => e.ConfidenceScore).HasMaxLength(16);
                entity.Property(e => e.IsAiSuggested).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            modelBuilder.Entity<HealthProviderServiceCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HealthProviderServiceId).HasMaxLength(128);
                entity.Property(e => e.HealthProviderServiceRelation).HasMaxLength(128).IsRequired();
                entity.Property(e => e.HealthProviderServiceName).HasMaxLength(512).IsRequired();
                entity.Property(e => e.NphiesServiceCode).HasMaxLength(128);
                entity.Property(e => e.IsMapped).HasDefaultValue(false);
            });

            modelBuilder.Entity<ServiceMappingSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CompletedAt);
            });
        }
    }
}