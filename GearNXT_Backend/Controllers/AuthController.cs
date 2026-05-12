using GearNXT_Backend.DTOs;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GearNXT_Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        // Post /api/auth/register-user
        [HttpPost("register-user")]
        public async Task<IActionResult> RegisterUser ([FromBody] RegisterRequestDto registerRequest)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var result = await _authService.RegisterAsync(registerRequest, userRole);
            return Ok(result);
        }


        // Post /api/auth/register-staff
        [Authorize(Roles = "Admin")]
        [HttpPost("register-staff")]
        public async Task<IActionResult> RegisterStaff ([FromBody] RegisterRequestDto registerRequest)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            registerRequest.Role = "Staff";

            var result = await _authService.RegisterAsync(registerRequest, userRole);
            return Ok(result);
        }


        // Post /api/login
        [HttpPost("login")]
        public async Task<IActionResult> Login ([FromBody] LoginRequestDto loginRequest)
        {
            var result = await _authService.LoginAsync(loginRequest);
            return Ok(result);
        }


            // GET /api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Extract userId from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                return Unauthorized("Invalid token.");

            var userId = int.Parse(userIdClaim);
            var result = await _authService.GetCurrentUserAsync(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] ProfileUpdateDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            try
            {
                var result = await _authService.UpdateCurrentUserAsync(userId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
