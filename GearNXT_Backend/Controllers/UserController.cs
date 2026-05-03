using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
namespace GearNXT_Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserService _userService;

        public AdminController(UserService userService)
        {
            _userService = userService;
        }

        // GET /api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> ListUsers([FromQuery] Role? role)
        {
            var users = await _userService.ListUsersAsync(role);

            if (!users.Any())
                return NotFound("No users found with the specified role.");

            return Ok(users);
        }
    }
}
