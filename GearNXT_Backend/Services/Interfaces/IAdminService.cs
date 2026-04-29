using System;
using GearNXT_Backend.DTOs.Auth;
using GearNXT_Backend.Models;

namespace GearNXT_Backend.Services.Interfaces;

public interface IAdminService
{
    Task <List<UserDto>> ListUsersAsync(string role);
}
