using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Interfaces;

namespace NphiesBridge.API.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthProviderAPIController : ControllerBase
    {
        private readonly IHealthProviderRepository _repository;

        public HealthProviderAPIController(IHealthProviderRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var providers = await _repository.GetAllAsync();
            return Ok(providers);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HealthProvider provider)
        {
            await _repository.AddAsync(provider);
            return CreatedAtAction(nameof(GetAll), new { id = provider.Id }, provider);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] HealthProvider provider)
        {
            if (id != provider.Id) return BadRequest();
            await _repository.UpdateAsync(provider);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
