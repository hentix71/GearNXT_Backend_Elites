using System;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using GearNXT_Backend.Models;
using Microsoft.EntityFrameworkCore;
using GearNXT_Backend.Data;
using GearNXT_Backend.Helpers;
using GearNXT_Backend.DTOs.Staff;
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
    private static StaffDto MapToStaffDto(User user) => new()
    {
        Name = user.Name,
        Email = user.Email,
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

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return null;

        return MapToDto(user);
    }
    public async Task<string> ToggleUserIsActiveAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            throw new Exception("User not found.");

        user.IsActive = !user.IsActive; // Toggle active status
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ("Change user active status successfully to " + user.IsActive);
    }
    public async Task<StaffDto> UpdateStaffAsync(int id, StaffDto updateRequest)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            throw new KeyNotFoundException("User not found.");
        if (user.Role != Role.Staff)
            throw new ArgumentException("User is not a staff member.");

        if (!string.IsNullOrWhiteSpace(updateRequest.Name))
        {
            var nameExists = await _db.Users.AnyAsync(u => u.Name == updateRequest.Name);
            if (nameExists && user.Name != updateRequest.Name)
                throw new InvalidOperationException("Name is already taken by another user.");
            user.Name = updateRequest.Name;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Email))
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == updateRequest.Email);
            if (emailExists && user.Email != updateRequest.Email)
                throw new InvalidOperationException("Email is already taken by another user.");
            user.Email = updateRequest.Email;
        }

        if (!string.IsNullOrWhiteSpace(updateRequest.Phone))
        {
            var phoneExists = await _db.Users.AnyAsync(u => u.Phone == updateRequest.Phone);
            if (phoneExists && user.Phone != updateRequest.Phone)
                throw new InvalidOperationException("Phone number is already taken by another user.");
            user.Phone = updateRequest.Phone;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToStaffDto(user);
    }
}
