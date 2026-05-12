using System;
using GearNXT_Backend.DTOs.Auth;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Services.Interfaces;

public interface IAuthService
{
    Task <AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest, string? userRole = null);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest);

    Task <UserDto> GetCurrentUserAsync(int userId);

    Task<UserDto> UpdateCurrentUserAsync(int userId, ProfileUpdateDto request);
}
