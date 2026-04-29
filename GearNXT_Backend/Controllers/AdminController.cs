using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;

namespace GearNXT_Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET /api/admin/users/role/{role}
        [HttpGet("users/role/{role}")]
        public async Task<IActionResult> ListUsers([FromRoute] string role)
        {
            var users = await _adminService.ListUsersAsync(role);

            if (!users.Any())
                return NotFound("No users found with the specified role.");

            return Ok(users);
        }
    }
}
