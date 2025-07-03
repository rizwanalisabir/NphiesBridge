using NphiesBridge.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NphiesBridge.Core.Interfaces
{
    public interface IHealthProviderRepository
    {
        Task<List<HealthProvider>> GetAllAsync();
        Task<HealthProvider?> GetByIdAsync(Guid id);
        Task AddAsync(HealthProvider provider);
        Task UpdateAsync(HealthProvider provider);
        Task DeleteAsync(Guid id);
    }
}
