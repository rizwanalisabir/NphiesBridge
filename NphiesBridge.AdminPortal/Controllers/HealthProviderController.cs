using Microsoft.AspNetCore.Mvc;
using NphiesBridge.AdminPortal.Services.API;
using NphiesBridge.Core.Entities;

namespace NphiesBridge.AdminPortal.Controllers
{
    public class HealthProviderController : Controller
    {
        private readonly HealthProviderApiService _apiService;

        public HealthProviderController(HealthProviderApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAllAsync();
            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(HealthProvider model)
        {
            if (!ModelState.IsValid) return View(model);
            await _apiService.AddAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // EDIT: GET
        public async Task<IActionResult> Edit(Guid id)
        {
            var provider = await _apiService.GetByIdAsync(id);
            if (provider == null) return NotFound();
            return View(provider);
        }

        // EDIT: POST
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, HealthProvider model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            await _apiService.UpdateAsync(id, model);
            return RedirectToAction(nameof(Index));
        }


        // DELETE: GET
        public async Task<IActionResult> Delete(Guid id)
        {
            var provider = await _apiService.GetByIdAsync(id);
            if (provider == null) return NotFound();
            return View(provider);
        }

        // DELETE: POST
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _apiService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
