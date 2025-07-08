using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NphiesBridge.Core.Entities;
using NphiesBridge.Infrastructure.Data;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.API.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UserAPIController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserAPIController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userManager.Users
                    .Include(u => u.HealthProvider)
                    .OrderBy(u => u.FirstName)
                    .ToListAsync();

                var response = new List<UserResponseDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    response.Add(new UserResponseDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        Role = roles.FirstOrDefault() ?? "Provider",
                        HealthProviderId = user.HealthProviderId,
                        HealthProviderName = user.HealthProvider?.Name,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        EmailConfirmed = user.EmailConfirmed
                    });
                }

                return Ok(ApiResponse<List<UserResponseDto>>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<UserResponseDto>>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.HealthProvider)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(ApiResponse<UserResponseDto>.ErrorResult("User not found"));

                var roles = await _userManager.GetRolesAsync(user);

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Provider",
                    HealthProviderId = user.HealthProviderId,
                    HealthProviderName = user.HealthProvider?.Name,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed
                };

                return Ok(ApiResponse<UserResponseDto>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UserResponseDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<UserResponseDto>.ErrorResult(errors));
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<UserResponseDto>.ErrorResult("User with this email already exists"));
                }

                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    HealthProviderId = dto.HealthProviderId,
                    IsActive = dto.IsActive,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse<UserResponseDto>.ErrorResult(errors));
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, dto.Role);

                // Reload user with HealthProvider
                user = await _userManager.Users
                    .Include(u => u.HealthProvider)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = dto.Role,
                    HealthProviderId = user.HealthProviderId,
                    HealthProviderName = user.HealthProvider?.Name,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed
                };

                return CreatedAtAction(nameof(GetById), new { id = user.Id },
                    ApiResponse<UserResponseDto>.SuccessResult(response, "User created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UserResponseDto>.ErrorResult($"User creation failed: {ex.Message}"));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(ApiResponse.ErrorResult("ID mismatch"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                    return NotFound(ApiResponse.ErrorResult("User not found"));

                // Check if email is taken by another user
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return BadRequest(ApiResponse.ErrorResult("Email is already taken by another user"));
                }

                // Update user properties
                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.Email = dto.Email;
                user.UserName = dto.Email;
                user.HealthProviderId = dto.HealthProviderId;
                user.IsActive = dto.IsActive;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                // Update role
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);

                return Ok(ApiResponse.SuccessResult("User updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult($"User update failed: {ex.Message}"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                    return NotFound(ApiResponse.ErrorResult("User not found"));

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                return Ok(ApiResponse.SuccessResult("User deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult($"User deletion failed: {ex.Message}"));
            }
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangeUserPasswordDto dto)
        {
            try
            {
                if (id != dto.UserId)
                    return BadRequest(ApiResponse.ErrorResult("ID mismatch"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                    return NotFound(ApiResponse.ErrorResult("User not found"));

                // Remove current password and set new one
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                return Ok(ApiResponse.SuccessResult("Password changed successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult($"Password change failed: {ex.Message}"));
            }
        }
    }
}