using System;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.Helpers;

namespace GearNXT_Backend.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public UserService(AppDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }
    
    // Private helper — converts User model to UserDto
    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role.ToString(),
        Phone = user.Phone
    };

    public async Task <List<UserDto>> ListUsersAsync(Role? filterRole)
    {
        if (filterRole == null)
        {
            return await _db.Users
                .Select(u => MapToDto(u))
                .ToListAsync();
        }
        return await _db.Users
            .Where(u => u.Role == filterRole)
            .Select(u => MapToDto(u))
            .ToListAsync();
    }
}
