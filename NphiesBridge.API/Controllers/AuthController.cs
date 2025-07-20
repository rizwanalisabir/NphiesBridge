using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NphiesBridge.API.Services;
using NphiesBridge.Core.Entities;
using NphiesBridge.Shared.DTOs;

namespace NphiesBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("Errors", errors));
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !user.IsActive)
                {
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("Invalid email or password"));
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                {
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("Invalid email or password"));
                }

                var roles = await _userManager.GetRolesAsync(user);
                var token = await _jwtService.GenerateTokenAsync(user, roles);

                var response = new AuthResponseDto
                {
                    Success = true,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Should match JWT expiration
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        Roles = roles.ToList(),
                        HealthProviderId = user.HealthProviderId
                    }
                };

                return Ok(ApiResponse<AuthResponseDto>.SuccessResult(response, "Login successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult($"Login failed: {ex.Message}"));
            }
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")] // Only admins can register new users
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("Errors", errors));
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("User with this email already exists"));
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    HealthProviderId = model.HealthProviderId,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse<AuthResponseDto>.ErrorResult("Errors", errors));
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, model.Role);

                var roles = await _userManager.GetRolesAsync(user);
                var token = await _jwtService.GenerateTokenAsync(user, roles);

                var response = new AuthResponseDto
                {
                    Success = true,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    User = new UserInfoDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Roles = roles.ToList(),
                        HealthProviderId = user.HealthProviderId
                    }
                };

                return Ok(ApiResponse<AuthResponseDto>.SuccessResult(response, "User registered successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult($"Registration failed: {ex.Message}"));
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(ApiResponse<UserInfoDto>.ErrorResult("Invalid user"));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(ApiResponse<UserInfoDto>.ErrorResult("User not found"));
                }

                var roles = await _userManager.GetRolesAsync(user);

                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList(),
                    HealthProviderId = user.HealthProviderId
                };

                return Ok(ApiResponse<UserInfoDto>.SuccessResult(userInfo));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UserInfoDto>.ErrorResult($"Failed to get profile: {ex.Message}"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(ApiResponse.ErrorResult("User not found"));
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(ApiResponse.ErrorResult(errors));
                }

                return Ok(ApiResponse.SuccessResult("Password changed successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResult($"Failed to change password: {ex.Message}"));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(ApiResponse.SuccessResult("Logged out successfully"));
        }
    }
}