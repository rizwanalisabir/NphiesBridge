using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Infrastructure.Data;

namespace NphiesBridge.Infrastructure.Repositories
{
    public class HealthProviderRepository : IHealthProviderRepository
    {
        private readonly ApplicationDbContext _context;

        public HealthProviderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HealthProvider>> GetAllAsync()
        {
            return await _context.HealthProviders
                .AsNoTracking()
                .OrderBy(x => x.Name) // Optional: sort by name
                .ToListAsync();
        }

        public async Task<HealthProvider?> GetByIdAsync(Guid id)
        {
            return await _context.HealthProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(HealthProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            await _context.HealthProviders.AddAsync(provider);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(HealthProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var existing = await _context.HealthProviders.FindAsync(provider.Id);
            if (existing == null)
                throw new InvalidOperationException("Health provider not found.");

            // Only update changed fields (optional)
            _context.Entry(existing).CurrentValues.SetValues(provider);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var provider = await _context.HealthProviders.FindAsync(id);
            if (provider == null)
                throw new InvalidOperationException("Health provider not found.");

            _context.HealthProviders.Remove(provider);
            await _context.SaveChangesAsync();
        }
    }
}
