using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using GearNXT_Backend.Models;
using GearNXT_Backend.Services;
using GearNXT_Backend.DTOs.Staff;
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


        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }


        [HttpPatch("users/{id}/toggle-active")]
        public async Task<IActionResult> ToggleUserIsActive(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            var result = await _userService.ToggleUserIsActiveAsync(id);

            return Ok(result);
        }

        [HttpPatch("staff/{id}/update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] StaffDto updateRequest)
        {

            var result = await _userService.UpdateStaffAsync(id, updateRequest);

            if (result == null)
                return BadRequest("Error updating staff member.");

            return Ok(result);
        }
    }
}
