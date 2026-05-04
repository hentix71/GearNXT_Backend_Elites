using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using GearNXT_Backend.Models;
using GearNXT_Backend.Helpers;
using GearNXT_Backend.Data;
using GearNXT_Backend.DTOs.Vendor;

namespace GearNXT_Backend.Services;

public class VendorService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;

    public VendorService(AppDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public static ResponseVendorDto MapToDto(Vendor vendor) => new()
    {
        Name = vendor.Name,
        Email = vendor.Email,
        Phone = vendor.Phone,
        Address = vendor.Address,
        IsActive = vendor.IsActive,
        CreatedAt = vendor.CreatedAt,
        UpdatedAt = vendor.UpdatedAt
    };

    public static VendorDto MapToVendorDto(Vendor vendor) => new()
    {
        Name = vendor.Name,
        Email = vendor.Email,
        Phone = vendor.Phone,
        Address = vendor.Address,
        IsActive = vendor.IsActive
    };

    public async Task<ResponseVendorDto> GetVendorByIdAsync(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null)
            throw new ArgumentException("Vendor not found.");
        return MapToDto(vendor);
    }

    public async Task<string> ToggleVendorIsActiveAsync(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null)
            throw new ArgumentException("Vendor not found.");

        vendor.IsActive = !vendor.IsActive;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return($"Changed vendor '{vendor.Name}' status to {(vendor.IsActive)}.");
    }

    public async Task<List<ResponseVendorDto>> ListVendorsAsync()
    {
        var vendors = await _db.Vendors.ToListAsync();
        return vendors.Select(MapToDto).ToList();
    }

    public async Task<List<ResponseVendorDto>> ListActiveVendorsAsync()
    {
        var vendors = await _db.Vendors.Where(v => v.IsActive).ToListAsync();
        return vendors.Select(MapToDto).ToList();
    }

    public async Task<VendorDto> UpdateVendorAsync(int id, VendorDto vendorDto)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null)
            throw new ArgumentException("Vendor not found.");

        // Check for unique name if changed
        if (!string.IsNullOrWhiteSpace(vendorDto.Name))
        {
            var nameExists = await _db.Vendors.AnyAsync(v => v.Name == vendorDto.Name);
            if (nameExists)
                throw new InvalidOperationException("Vendor name already exists.");
            
            vendor.Name = vendorDto.Name;
        }

        // Check for unique email if changed
        if (!string.IsNullOrWhiteSpace(vendorDto.Email))
        {
            var emailExists = await _db.Vendors.AnyAsync(v => v.Email == vendorDto.Email);
            if (emailExists)
                throw new InvalidOperationException("Email already registered.");
            
            vendor.Email = vendorDto.Email;
        }

        // Check for unique phone if changed
        if (!string.IsNullOrWhiteSpace(vendorDto.Phone))
        {
            var phoneExists = await _db.Vendors.AnyAsync(v => v.Phone == vendorDto.Phone);
            if (phoneExists)
                throw new InvalidOperationException("Phone number already registered.");
            
            vendor.Phone = vendorDto.Phone;
        }

        // Update address
        if (!string.IsNullOrWhiteSpace(vendorDto.Address))
        {
            vendor.Address = vendorDto.Address;
        }

        // Update timestamp
        vendor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToVendorDto(vendor);    
    }

    public async Task<ResponseVendorDto> RegisterVendorAsync(RegisterVendorDto vendorDto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(vendorDto.Name) ||
            string.IsNullOrWhiteSpace(vendorDto.Email) ||
            string.IsNullOrWhiteSpace(vendorDto.Phone) ||
            string.IsNullOrWhiteSpace(vendorDto.Address))
        {
            throw new ArgumentException("All fields are required.");
        }
        // Check if vendor name already exists
        var nameExists = await _db.Vendors.AnyAsync(v => v.Name == vendorDto.Name);
        if (nameExists)            
            throw new InvalidOperationException("Vendor name already exists.");
        // Check if email already exists
        var emailExists = await _db.Vendors.AnyAsync(v => v.Email == vendorDto.Email);
        if (emailExists)
            throw new InvalidOperationException("Email already registered.");
        // Check if phone already exists
        var phoneExists = await _db.Vendors.AnyAsync(v => v.Phone == vendorDto.Phone);
        if (phoneExists)
            throw new InvalidOperationException("Phone number already registered.");
        
        var vendor = new Vendor
        {
            Name = vendorDto.Name,
            Email = vendorDto.Email,
            Phone = vendorDto.Phone,
            Address = vendorDto.Address,
            IsActive = true
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        
        return MapToDto(vendor);
    }
}
