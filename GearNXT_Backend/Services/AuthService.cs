using System;
using System.Linq;
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
        Phone = user.Phone,
        EmployeeNumber = null
    };

    private async Task<string> GenerateNextEmployeeNumberAsync()
    {
        const string prefix = "GNX-STF-";
        var last = await _db.StaffProfiles
            .Where(s => s.EmployeeNumber != null && s.EmployeeNumber.StartsWith(prefix))
            .OrderByDescending(s => s.Id)
            .Select(s => s.EmployeeNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (!string.IsNullOrWhiteSpace(last) && last.Length > prefix.Length)
        {
            var suffix = last.Substring(prefix.Length);
            if (int.TryParse(suffix, out var n)) next = n + 1;
        }
        return $"{prefix}{next:D4}";
    }

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

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Create the user
        var user = new User
        {
            Name = registerRequest.Name.Trim(),
            Email = registerRequest.Email.Trim(),
            PasswordHash = hash,
            Role = role,
            Phone = registerRequest.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (role == Role.Customer)
        {
            // Create a thin customer profile linked to the new user.
            var existing = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (existing == null)
            {
                _db.Customers.Add(new Customer
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }

        // Create thin StaffProfile when registering a staff user (admin-only registration enforced earlier)
        if (role == Role.Staff)
        {
            var existingStaff = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (existingStaff != null)
            {
                // already linked; ensure basic fields updated if needed
            }
            else
            {
                var empNum = await GenerateNextEmployeeNumberAsync();
                _db.StaffProfiles.Add(new StaffProfile
                {
                    UserId = user.Id,
                    EmployeeNumber = empNum
                });
                await _db.SaveChangesAsync();
            }
        }

        // Create thin AdminProfile when registering an admin user (admin-only registration enforced earlier)
        if (role == Role.Admin)
        {
            var existingAdmin = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (existingAdmin != null)
            {
                // already linked
            }
            else
            {
                _db.AdminProfiles.Add(new AdminProfile
                {
                    UserId = user.Id
                });
                await _db.SaveChangesAsync();
            }
        }

        await transaction.CommitAsync();

        // Generate JWT Token
        var token = _jwt.GenerateToken(user);
        var userDto = MapToDto(user);
        // populate employee number if staff
        var emp = await _db.StaffProfiles.Where(s => s.UserId == user.Id).Select(s => s.EmployeeNumber).FirstOrDefaultAsync();
        userDto.EmployeeNumber = emp;

        return new AuthResponseDto
        {
            Token = token,
            User = userDto
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
            Console.WriteLine("Invalid Password");
            throw new ArgumentException("Invalid Email or Password");
        }

        // Generate token and return
        var token = _jwt.GenerateToken(user);
        var userDto = MapToDto(user);
        var emp = await _db.StaffProfiles.Where(s => s.UserId == user.Id).Select(s => s.EmployeeNumber).FirstOrDefaultAsync();
        userDto.EmployeeNumber = emp;
        return new AuthResponseDto
        {
            Token = token,
            User = userDto
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

        var dto = MapToDto(user);
        var emp = await _db.StaffProfiles.Where(s => s.UserId == user.Id).Select(s => s.EmployeeNumber).FirstOrDefaultAsync();
        dto.EmployeeNumber = emp;
        return dto;
    }
}
