using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NphiesBridge.Core.Entities;
using NphiesBridge.Core.Interfaces;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.API.Controllers.Admin
{
    [ApiController]
    [Route("api/healthprovider")]
    //[Authorize(Policy = "AdminOnly")]
    public class HealthProviderAPIController : ControllerBase
    {
        private readonly IHealthProviderRepository _repository;
        private readonly IValidator<CreateHealthProviderDto> _createValidator;
        private readonly IValidator<UpdateHealthProviderDto> _updateValidator;

        public HealthProviderAPIController(
            IHealthProviderRepository repository,
            IValidator<CreateHealthProviderDto> createValidator,
            IValidator<UpdateHealthProviderDto> updateValidator)
        {
            _repository = repository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var providers = await _repository.GetAllAsync();
                var response = providers.Select(p => new HealthProviderResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    LicenseNumber = p.LicenseNumber,
                    ContactPerson = p.ContactPerson,
                    Email = p.Email,
                    Phone = p.Phone,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                }).ToList();

                return Ok(ApiResponse<List<HealthProviderResponseDto>>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<HealthProviderResponseDto>>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var provider = await _repository.GetByIdAsync(id);
                if (provider == null)
                    return NotFound(ApiResponse<HealthProviderResponseDto>.ErrorResult("Provider not found"));

                var response = new HealthProviderResponseDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    LicenseNumber = provider.LicenseNumber,
                    ContactPerson = provider.ContactPerson,
                    Email = provider.Email,
                    Phone = provider.Phone,
                    IsActive = provider.IsActive,
                    CreatedAt = provider.CreatedAt
                };

                return Ok(ApiResponse<HealthProviderResponseDto>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<HealthProviderResponseDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHealthProviderDto dto)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<HealthProviderResponseDto>.ErrorResult(errors));
                }

                var provider = new HealthProvider
                {
                    Name = dto.Name,
                    LicenseNumber = dto.LicenseNumber,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive
                };

                await _repository.AddAsync(provider);

                var response = new HealthProviderResponseDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    LicenseNumber = provider.LicenseNumber,
                    ContactPerson = provider.ContactPerson,
                    Email = provider.Email,
                    Phone = provider.Phone,
                    IsActive = provider.IsActive,
                    CreatedAt = provider.CreatedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = provider.Id },
                    ApiResponse<HealthProviderResponseDto>.SuccessResult(response, "Provider created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<HealthProviderResponseDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHealthProviderDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(ApiResponse.ErrorResult("ID mismatch"));

                var validationResult = await _updateValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                var provider = new HealthProvider
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    LicenseNumber = dto.LicenseNumber,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = dto.IsActive
                };

                await _repository.UpdateAsync(provider);
                return Ok(ApiResponse.SuccessResult("Provider updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _repository.DeleteAsync(id);
                return Ok(ApiResponse.SuccessResult("Provider deleted successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult(ex.Message));
            }
        }
    }
}