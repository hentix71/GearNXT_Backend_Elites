using AutoMapper;
using GearNXT_Backend.DTOs.Customer;
using GearNXT_Backend.Models;
using System.Linq;
using System.Globalization;

namespace GearNXT_Backend.DTOs.Mapping;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Vehicle, VehicleDto>()
            .ForMember(dest => dest.RegistrationNumber, opt => opt.MapFrom(src => src.LicensePlate))
            .ForMember(dest => dest.Vin, opt => opt.MapFrom(src => src.VehicleNumber));

        CreateMap<SalesInvoiceItem, InvoiceItemDto>()
            .ForMember(dest => dest.PartName, opt => opt.MapFrom(src => src.PartName ?? (src.Part != null ? src.Part.Name : string.Empty)))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Quantity * src.UnitPrice));

        CreateMap<SalesInvoice, InvoiceDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(_ => "Purchase"))
            .ForMember(dest => dest.Item, opt => opt.MapFrom(src => string.Join(", ", src.Items.Select(i => (i.PartName ?? (i.Part != null ? i.Part.Name : "Part")) + " x" + i.Quantity))))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.GrandTotal))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.PaymentStatus))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<GearNXT_Backend.Models.Customer, CustomerSummaryDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone ?? string.Empty : string.Empty))
            .ForMember(dest => dest.RegisteredDate, opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(dest => dest.TotalSpend, opt => opt.MapFrom(src => src.SalesInvoices.Sum(si => si.GrandTotal)))
            .ForMember(dest => dest.CreditBalance, opt => opt.MapFrom(src => src.SalesInvoices.Where(si => si.PaymentStatus == "Credit").Sum(si => si.GrandTotal - si.PaidAmount)))
            .ForMember(dest => dest.LastVisit, opt => opt.MapFrom(src => src.SalesInvoices.Any()
                ? src.SalesInvoices.OrderByDescending(si => si.InvoiceDate).First().InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty))
            .ForMember(dest => dest.Vehicle, opt => opt.MapFrom(src => src.Vehicles.OrderByDescending(v => v.CreatedAt).FirstOrDefault()))
            .ForMember(dest => dest.Vehicles, opt => opt.MapFrom(src => src.Vehicles));
    }
}
