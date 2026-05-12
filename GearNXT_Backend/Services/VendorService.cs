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
        Id = vendor.Id,
        Name = vendor.Name,
        VendorCode = vendor.VendorCode,
        ContactPerson = vendor.ContactPerson,
        ContactTitle = vendor.ContactTitle,
        Email = vendor.Email,
        Phone = vendor.Phone,
        AlternatePhone = vendor.AlternatePhone,
        Website = vendor.Website,
        Address = vendor.Address,
        City = vendor.City,
        District = vendor.District,
        Country = vendor.Country,
        PanNumber = vendor.PanNumber,
        PaymentTerms = vendor.PaymentTerms,
        BankName = vendor.BankName,
        BankAccount = vendor.BankAccount,
        ProductCategories = vendor.ProductCategories,
        Notes = vendor.Notes,
        IsActive = vendor.IsActive,
        CreatedAt = vendor.CreatedAt,
        UpdatedAt = vendor.UpdatedAt
    };

    public static VendorDto MapToVendorDto(Vendor vendor) => new()
    {
        Name = vendor.Name,
        VendorCode = vendor.VendorCode,
        ContactPerson = vendor.ContactPerson,
        ContactTitle = vendor.ContactTitle,
        Email = vendor.Email,
        Phone = vendor.Phone,
        AlternatePhone = vendor.AlternatePhone,
        Website = vendor.Website,
        Address = vendor.Address,
        City = vendor.City,
        District = vendor.District,
        Country = vendor.Country,
        PanNumber = vendor.PanNumber,
        PaymentTerms = vendor.PaymentTerms,
        BankName = vendor.BankName,
        BankAccount = vendor.BankAccount,
        ProductCategories = vendor.ProductCategories,
        Notes = vendor.Notes,
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
        var vendors = await _db.Vendors
            .Include(v => v.Parts)
            .Include(v => v.PurchaseInvoices)
            .OrderBy(v => v.Name)
            .ToListAsync();
        return vendors.Select(MapToDto).ToList();
    }

    public async Task<List<ResponseVendorDto>> ListActiveVendorsAsync()
    {
        var vendors = await _db.Vendors
            .Include(v => v.Parts)
            .Include(v => v.PurchaseInvoices)
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToListAsync();
        return vendors.Select(MapToDto).ToList();
    }

    public async Task<VendorDto> UpdateVendorAsync(int id, VendorDto vendorDto)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null)
            throw new ArgumentException("Vendor not found.");

        if (!string.IsNullOrWhiteSpace(vendorDto.Name))
        {
            var name = vendorDto.Name.Trim();
            var nameExists = await _db.Vendors.AnyAsync(v => v.Id != id && v.Name == name);
            if (nameExists)
                throw new InvalidOperationException("Vendor name already exists.");
            vendor.Name = name;
        }

        if (vendorDto.VendorCode != null)
        {
            var code = string.IsNullOrWhiteSpace(vendorDto.VendorCode)
                ? null
                : vendorDto.VendorCode.Trim().ToUpperInvariant();
            if (code != null)
            {
                var codeExists = await _db.Vendors.AnyAsync(
                    v => v.Id != id && v.VendorCode == code);
                if (codeExists)
                    throw new InvalidOperationException("Vendor code already exists.");
            }
            vendor.VendorCode = code;
        }

        if (!string.IsNullOrWhiteSpace(vendorDto.Email))
        {
            var email = vendorDto.Email.Trim();
            var emailExists = await _db.Vendors.AnyAsync(v => v.Id != id && v.Email == email);
            if (emailExists)
                throw new InvalidOperationException("Email already registered.");
            vendor.Email = email;
        }

        if (!string.IsNullOrWhiteSpace(vendorDto.Phone))
        {
            var phone = vendorDto.Phone.Trim();
            var phoneExists = await _db.Vendors.AnyAsync(v => v.Id != id && v.Phone == phone);
            if (phoneExists)
                throw new InvalidOperationException("Phone number already registered.");
            vendor.Phone = phone;
        }

        vendor.ContactPerson = string.IsNullOrWhiteSpace(vendorDto.ContactPerson)
            ? vendor.ContactPerson
            : vendorDto.ContactPerson.Trim();
        vendor.ContactTitle = TrimOrNull(vendorDto.ContactTitle);
        vendor.AlternatePhone = TrimOrNull(vendorDto.AlternatePhone);
        vendor.Website = TrimOrNull(vendorDto.Website);
        vendor.Address = string.IsNullOrWhiteSpace(vendorDto.Address)
            ? vendor.Address
            : vendorDto.Address.Trim();
        vendor.City = string.IsNullOrWhiteSpace(vendorDto.City)
            ? vendor.City
            : vendorDto.City.Trim();
        vendor.District = TrimOrNull(vendorDto.District);
        vendor.Country = string.IsNullOrWhiteSpace(vendorDto.Country)
            ? vendor.Country
            : vendorDto.Country.Trim();
        vendor.PanNumber = TrimOrNull(vendorDto.PanNumber)?.ToUpperInvariant();
        vendor.PaymentTerms = TrimOrNull(vendorDto.PaymentTerms);
        vendor.BankName = TrimOrNull(vendorDto.BankName);
        vendor.BankAccount = TrimOrNull(vendorDto.BankAccount);
        vendor.ProductCategories = TrimOrNull(vendorDto.ProductCategories);
        vendor.Notes = TrimOrNull(vendorDto.Notes);

        vendor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToVendorDto(vendor);
    }

    public async Task<ResponseVendorDto> RegisterVendorAsync(RegisterVendorDto vendorDto)
    {
        if (string.IsNullOrWhiteSpace(vendorDto.Name) ||
            string.IsNullOrWhiteSpace(vendorDto.ContactPerson) ||
            string.IsNullOrWhiteSpace(vendorDto.Email) ||
            string.IsNullOrWhiteSpace(vendorDto.Phone) ||
            string.IsNullOrWhiteSpace(vendorDto.Address) ||
            string.IsNullOrWhiteSpace(vendorDto.City))
        {
            throw new ArgumentException(
                "Company name, contact person, email, phone, street address, and city are required.");
        }

        var name = vendorDto.Name.Trim();
        var email = vendorDto.Email.Trim();
        var phone = vendorDto.Phone.Trim();
        var vendorCode = string.IsNullOrWhiteSpace(vendorDto.VendorCode)
            ? null
            : vendorDto.VendorCode.Trim().ToUpperInvariant();

        if (await _db.Vendors.AnyAsync(v => v.Name == name))
            throw new InvalidOperationException("Vendor name already exists.");
        if (await _db.Vendors.AnyAsync(v => v.Email == email))
            throw new InvalidOperationException("Email already registered.");
        if (await _db.Vendors.AnyAsync(v => v.Phone == phone))
            throw new InvalidOperationException("Phone number already registered.");
        if (vendorCode != null && await _db.Vendors.AnyAsync(v => v.VendorCode == vendorCode))
            throw new InvalidOperationException("Vendor code already exists.");

        var vendor = new Vendor
        {
            Name = name,
            VendorCode = vendorCode,
            ContactPerson = vendorDto.ContactPerson.Trim(),
            ContactTitle = TrimOrNull(vendorDto.ContactTitle),
            Email = email,
            Phone = phone,
            AlternatePhone = TrimOrNull(vendorDto.AlternatePhone),
            Website = TrimOrNull(vendorDto.Website),
            Address = vendorDto.Address.Trim(),
            City = vendorDto.City.Trim(),
            District = TrimOrNull(vendorDto.District),
            Country = string.IsNullOrWhiteSpace(vendorDto.Country)
                ? "Nepal"
                : vendorDto.Country.Trim(),
            PanNumber = TrimOrNull(vendorDto.PanNumber)?.ToUpperInvariant(),
            PaymentTerms = TrimOrNull(vendorDto.PaymentTerms) ?? "Net 30 days",
            BankName = TrimOrNull(vendorDto.BankName),
            BankAccount = TrimOrNull(vendorDto.BankAccount),
            ProductCategories = TrimOrNull(vendorDto.ProductCategories),
            Notes = TrimOrNull(vendorDto.Notes),
            IsActive = true
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        return MapToDto(vendor);
    }

    public async Task<List<Part>?> GetVendorPartsAsync(int vendorId)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Id == vendorId);
        if (!exists)
        {
            return null;
        }

        return await _db.Parts.Where(p => p.VendorId == vendorId).ToListAsync();
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
