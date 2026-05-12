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
        Phone = user.Phone,
        IsActive = user.IsActive,
        EmployeeNumber = null
    };
    private static StaffDto MapToStaffDto(User user) => new()
    {
        Name = user.Name,
        Email = user.Email,
        Phone = user.Phone
    };

    private async Task<Dictionary<int, string?>> GetStaffEmployeeNumbersAsync()
    {
        return await _db.StaffProfiles
            .Where(s => s.UserId.HasValue)
            .ToDictionaryAsync(s => s.UserId!.Value, s => s.EmployeeNumber);
    }

    public async Task <List<UserDto>> ListUsersAsync(Role? filterRole)
    {
        var staffMap = await GetStaffEmployeeNumbersAsync();

        if (filterRole == null)
        {
            var allUsers = await _db.Users.ToListAsync();
            return allUsers.Select(u => {
                var dto = MapToDto(u);
                if (u.Role == Role.Staff && staffMap.TryGetValue(u.Id, out var emp)) dto.EmployeeNumber = emp;
                return dto;
            }).ToList();
        }

        var filteredUsers = await _db.Users.Where(u => u.Role == filterRole).ToListAsync();
        return filteredUsers.Select(u => {
            var dto = MapToDto(u);
            if (u.Role == Role.Staff && staffMap.TryGetValue(u.Id, out var emp)) dto.EmployeeNumber = emp;
            return dto;
        }).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return null;
        var dto = MapToDto(user);
        var emp = await _db.Set<StaffProfile>().Where(s => s.UserId == user.Id).Select(s => s.EmployeeNumber).FirstOrDefaultAsync();
        dto.EmployeeNumber = emp;
        return dto;
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
        var dto = MapToStaffDto(user);
        var emp = await _db.Set<StaffProfile>().Where(s => s.UserId == user.Id).Select(s => s.EmployeeNumber).FirstOrDefaultAsync();
        dto.EmployeeNumber = emp;
        return dto;
    }

    public async Task ResetStaffPasswordAsync(int id, AdminResetPasswordDto request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Role != Role.Staff)
        {
            throw new ArgumentException("Password reset is only available for staff accounts.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
