using System;
using GearNXT_Backend.Services.Interfaces;
using GearNXT_Backend.DTOs.Auth;
using GearNXT_Backend.Models;
using GearNXT_Backend.Data;
using GearNXT_Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthService(AppDbContext db, JwtHelper jwt)
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

    public async Task <AuthResponseDto> RegisterAsync (RegisterRequestDto registerRequest, string? userRole = null)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(registerRequest.Email) ||
            string.IsNullOrWhiteSpace(registerRequest.Password) ||
            string.IsNullOrWhiteSpace(registerRequest.ConfirmPassword))
        {
            throw new ArgumentException("Email, password, and confirm password are required.");
        }

        if (registerRequest.Password != registerRequest.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match."); 
        }

        // Checking if eamil alrady exist
        var exists = await _db.Users.AnyAsync(u => u.Email == registerRequest.Email);

        if (exists)
            throw new InvalidOperationException("Email already registered.");
        
        var nameExists = await _db.Users.AnyAsync(u => u.Name == registerRequest.Name);
        if (nameExists)
            throw new InvalidOperationException("Name already taken.");

        // Hash the password
        var hash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

        // For Role
        var role = Role.Customer;

        if (!string.IsNullOrWhiteSpace(registerRequest.Role))
        {
            var requestedRole = Enum.Parse<Role>(registerRequest.Role, ignoreCase: true);

            // Admin can only assign admin or staff role
            if ((requestedRole == Role.Staff || requestedRole == Role.Admin) 
                && userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admin can  assign Admin and Staff role");
            }

            role = requestedRole;
        }

        // Create the user
        var user = new User
        {
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            PasswordHash = hash,
            Role = role,
            Phone = registerRequest.Phone,
            CreatedAt = DateTime.UtcNow
        };

        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        

        // Generate JWT Tocken
        var token = _jwt.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            User = MapToDto(user)
        };
    }

    public async Task <AuthResponseDto> LoginAsync (LoginRequestDto loginRequest)
    {
        // Find user of that email
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
        if (user == null)
        {
            Console.WriteLine("User not found");
            throw new ArgumentException("Invalid Email or Password");
        }

        // verifiy password
        var valid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);
        if (!valid)
        {
            var newHash = BCrypt.Net.BCrypt.HashPassword("admin");
            Console.WriteLine($"Hash of 'admin': {newHash}");

            Console.WriteLine("Invalid Password");
            throw new ArgumentException("Invalid Email or Password");
        }

        // Generate token and return
        var token = _jwt.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            User = MapToDto(user)
        }; 
    }

    public async Task <UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            Console.WriteLine("User not found. Getting current user.");
            throw new KeyNotFoundException("User not found.");
        }

        return MapToDto(user);
    }
}
