using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NphiesBridge.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }
        public DbSet<HealthProvider> HealthProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HealthProvider>()
                .HasIndex(p => p.LicenseNumber)
                .IsUnique(false);
        }
    }
}
