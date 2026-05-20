using System.Globalization;
using GearNXT_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearNXT_Backend.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Staff")]
public class ReportsController : ControllerBase
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountPercent = 10m;

    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("financial")]
    public async Task<IActionResult> Financial([FromQuery] string range = "daily")
    {
        var now = DateTime.UtcNow;
        DateTime from = range.ToLower() switch
        {
            "yearly" => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "monthly" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => now.Date
        };

        var sales = await _db.SalesInvoices
            .Where(i => i.InvoiceDate >= from)
            .ToListAsync();

        var purchases = await _db.PurchaseInvoices
            .Where(i => i.InvoiceDate >= from)
            .ToListAsync();

        var totalSales = sales.Sum(i => i.GrandTotal);
        var totalPurchases = purchases.Sum(i => i.TotalAmount);
        var grossProfit = totalSales - totalPurchases;
        var margin = totalSales > 0 ? (grossProfit / totalSales) * 100m : 0m;

        var cashSales = sales.Where(i => i.PaymentStatus == "Paid").Sum(i => i.GrandTotal);
        var creditSales = sales.Where(i => i.PaymentStatus == "Credit").Sum(i => i.GrandTotal);

        var topSellingParts = await _db.SalesInvoiceItems
            .Include(item => item.Part)
            .Where(item => item.Invoice != null && item.Invoice.InvoiceDate >= from)
            .GroupBy(item => item.PartName ?? (item.Part != null ? item.Part.Name : "Unknown"))
            .Select(group => new
            {
                name = group.Key,
                quantity = group.Sum(x => x.Quantity),
                revenue = group.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.quantity)
            .Take(5)
            .ToListAsync();

        string Format(decimal value) => $"NPR {value.ToString("N0", CultureInfo.InvariantCulture)}";

        var payload = new Dictionary<string, object>
        {
            ["totalSales"] = Format(totalSales),
            ["totalPurchases"] = Format(totalPurchases),
            ["grossProfit"] = Format(grossProfit),
            ["profitMargin"] = $"{margin:0.#}%",
            ["topSellingParts"] = topSellingParts
        };

        if (range.Equals("daily", StringComparison.OrdinalIgnoreCase))
        {
            payload["todayRevenue"] = Format(totalSales);
            payload["todayPurchase"] = Format(totalPurchases);
            payload["netDailyProfit"] = Format(grossProfit);
            payload["cashSales"] = Format(cashSales);
            payload["creditSales"] = Format(creditSales);
        }
        else if (range.Equals("monthly", StringComparison.OrdinalIgnoreCase))
        {
            payload["monthlyRevenue"] = Format(totalSales);
            payload["monthlyPurchase"] = Format(totalPurchases);
            payload["monthlyNetProfit"] = Format(grossProfit);
            payload["monthlyCashSales"] = Format(cashSales);
            payload["monthlyCreditSales"] = Format(creditSales);
        }
        else
        {
            payload["yearlyRevenue"] = Format(totalSales);
            payload["yearlyPurchase"] = Format(totalPurchases);
            payload["yearlyNetProfit"] = Format(grossProfit);
            payload["yearlyCashSales"] = Format(cashSales);
            payload["yearlyCreditSales"] = Format(creditSales);
        }

        return Ok(payload);
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard([FromQuery] int appointmentDays = 7)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trendStart = today.AddMonths(-11);
        trendStart = new DateTime(trendStart.Year, trendStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allSales = await _db.SalesInvoices.AsNoTracking().ToListAsync();
        var allPurchases = await _db.PurchaseInvoices.AsNoTracking().ToListAsync();

        var todaySales = allSales.Where(i => i.InvoiceDate >= today).ToList();
        var todayPurchases = allPurchases.Where(i => i.InvoiceDate >= today).ToList();
        var todayRevenue = todaySales.Sum(i => i.GrandTotal);
        var todayPurchase = todayPurchases.Sum(i => i.TotalAmount);

        var monthSales = allSales.Where(i => i.InvoiceDate >= monthStart).ToList();
        var monthPurchases = allPurchases.Where(i => i.InvoiceDate >= monthStart).ToList();

        var creditInvoices = allSales
            .Where(i => i.PaymentStatus == "Credit")
            .ToList();

        var lowStockCount = await _db.Parts
            .CountAsync(p => p.IsActive && p.StockQuantity < 10);

        var customerCount = await _db.Customers.CountAsync();

        var topSellingParts = await _db.SalesInvoiceItems
            .Include(item => item.Part)
            .Where(item => item.Invoice != null && item.Invoice.InvoiceDate >= yearStart)
            .GroupBy(item => item.PartName ?? (item.Part != null ? item.Part.Name : "Unknown"))
            .Select(group => new
            {
                name = group.Key,
                quantity = group.Sum(x => x.Quantity),
                revenue = group.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.revenue)
            .Take(5)
            .ToListAsync();

        var monthlyTrend = new List<object>();
        for (var i = 0; i < 12; i++)
        {
            var monthDate = trendStart.AddMonths(i);
            var nextMonth = monthDate.AddMonths(1);
            var salesTotal = allSales
                .Where(inv => inv.InvoiceDate >= monthDate && inv.InvoiceDate < nextMonth)
                .Sum(inv => inv.GrandTotal);
            var purchaseTotal = allPurchases
                .Where(inv => inv.InvoiceDate >= monthDate && inv.InvoiceDate < nextMonth)
                .Sum(inv => inv.TotalAmount);

            monthlyTrend.Add(new
            {
                label = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                revenue = salesTotal,
                purchases = purchaseTotal,
                profit = salesTotal - purchaseTotal
            });
        }

        var appointmentCutoff = now;
        var appointmentEnd = now.AddDays(appointmentDays);
        var appointments = await _db.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
                .ThenInclude(c => c!.User)
            .Include(a => a.Customer)
                .ThenInclude(c => c!.Vehicles)
            .Where(a =>
                a.Status == "Upcoming" &&
                a.PreferredDate >= appointmentCutoff &&
                a.PreferredDate <= appointmentEnd)
            .OrderBy(a => a.PreferredDate)
            .Take(10)
            .ToListAsync();

        var loyaltyDiscounted = await _db.SalesInvoices
            .Where(i => i.DiscountApplied)
            .ToListAsync();

        string Format(decimal value) => $"NPR {value.ToString("N0", CultureInfo.InvariantCulture)}";

        return Ok(new
        {
            customerCount,
            lowStockCount,
            pendingCreditCustomers = creditInvoices.Select(i => i.CustomerId).Distinct().Count(),
            pendingCreditAmount = creditInvoices.Sum(i => i.GrandTotal - i.PaidAmount),
            totalRevenue = allSales.Sum(i => i.GrandTotal),
            daily = new
            {
                todayRevenue,
                todayPurchase,
                netDailyProfit = todayRevenue - todayPurchase,
                cashSales = todaySales.Where(i => i.PaymentStatus == "Paid").Sum(i => i.GrandTotal),
                creditSales = todaySales.Where(i => i.PaymentStatus == "Credit").Sum(i => i.GrandTotal),
                todayRevenueFormatted = Format(todayRevenue),
                todayPurchaseFormatted = Format(todayPurchase),
                netDailyProfitFormatted = Format(todayRevenue - todayPurchase)
            },
            monthly = new
            {
                revenue = monthSales.Sum(i => i.GrandTotal),
                purchases = monthPurchases.Sum(i => i.TotalAmount),
                profit = monthSales.Sum(i => i.GrandTotal) - monthPurchases.Sum(i => i.TotalAmount),
                revenueFormatted = Format(monthSales.Sum(i => i.GrandTotal)),
                profitFormatted = Format(monthSales.Sum(i => i.GrandTotal) - monthPurchases.Sum(i => i.TotalAmount))
            },
            monthlyTrend,
            topSellingParts,
            loyalty = new
            {
                totalDiscounts = loyaltyDiscounted.Sum(i => i.DiscountAmount),
                totalBeneficiaries = loyaltyDiscounted.Select(i => i.CustomerId).Distinct().Count(),
                invoicesWithDiscount = loyaltyDiscounted.Count
            },
            upcomingAppointments = appointments.Select(a =>
            {
                var vehicle = a.Customer?.Vehicles.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                var vehicleInfo = vehicle == null
                    ? ""
                    : $"{vehicle.Make} {vehicle.Model}".Trim() +
                      (vehicle.Year.HasValue && vehicle.Year > 0 ? $" ({vehicle.Year})" : "") +
                      (string.IsNullOrWhiteSpace(vehicle.LicensePlate) ? "" : $" — {vehicle.LicensePlate}");

                return new
                {
                    id = a.Id,
                    customerName = a.Customer?.User?.Name ?? "",
                    vehicleInfo,
                    type = a.ServiceType,
                    service = a.ServiceType,
                    date = a.PreferredDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    time = a.PreferredDate.ToString("HH:mm", CultureInfo.InvariantCulture),
                    status = a.Status
                };
            })
        });
    }

    [HttpGet("loyalty")]
    public async Task<IActionResult> Loyalty()
    {
        var discounted = await _db.SalesInvoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.User)
            .Where(i => i.DiscountApplied)
            .ToListAsync();

        var totalDiscounts = discounted.Sum(i => i.DiscountAmount);
        var beneficiaries = discounted
            .GroupBy(i => i.CustomerId)
            .Select(group => new
            {
                customer = group.First().Customer?.User?.Name ?? $"Customer #{group.Key}",
                invoices = group.Count(),
                totalSpend = group.Sum(i => i.GrandTotal),
                totalSaved = group.Sum(i => i.DiscountAmount)
            })
            .OrderByDescending(x => x.totalSaved)
            .Take(5)
            .ToList();

        return Ok(new
        {
            totalBeneficiaries = discounted.Select(i => i.CustomerId).Distinct().Count(),
            totalDiscounts,
            invoicesWithDiscount = discounted.Count,
            averageDiscount = discounted.Count == 0 ? 0 : totalDiscounts / discounted.Count,
            loyaltyThreshold = LoyaltyThreshold,
            loyaltyDiscountPercent = LoyaltyDiscountPercent,
            topBeneficiaries = beneficiaries
        });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers()
    {
        var customers = await _db.Customers
            .Include(c => c.User)
            .Include(c => c.Vehicles)
            .Include(c => c.SalesInvoices)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var result = customers.Select(c =>
        {
            var lastInvoice = c.SalesInvoices.OrderByDescending(i => i.InvoiceDate).FirstOrDefault();
            return new
            {
                id = c.Id,
                fullName = c.User?.Name ?? $"Customer #{c.Id}",
                name = c.User?.Name ?? $"Customer #{c.Id}",
                email = c.User?.Email ?? "",
                phone = c.User?.Phone ?? "",
                registeredDate = c.CreatedAt.ToString("yyyy-MM-dd"),
                createdAt = c.CreatedAt,
                totalSpend = c.SalesInvoices.Sum(i => i.GrandTotal),
                creditBalance = c.SalesInvoices
                    .Where(i => i.PaymentStatus == "Credit")
                    .Sum(i => i.GrandTotal - i.PaidAmount),
                lastVisit = lastInvoice != null
                    ? lastInvoice.InvoiceDate.ToString("yyyy-MM-dd")
                    : "",
                vehicle = c.Vehicles.Select(v => new
                {
                    make = v.Make,
                    model = v.Model,
                    year = v.Year,
                    registrationNumber = v.LicensePlate,
                    vin = v.VehicleNumber,
                    color = v.Color,
                    fuelType = v.FuelType
                }).FirstOrDefault()
            };
        });

        return Ok(result);
    }

    [HttpGet("customer-service-history")]
    public async Task<IActionResult> CustomerServiceHistory()
    {
        var invoices = await _db.SalesInvoices
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var history = invoices.Select(i => new
        {
            id = i.Id,
            customerId = i.CustomerId,
            type = "Purchase",
            item = i.InvoiceNumber,
            description = i.InvoiceNumber,
            amount = i.GrandTotal,
            total = i.GrandTotal,
            date = i.InvoiceDate.ToString("yyyy-MM-dd"),
            createdAt = i.InvoiceDate,
            status = i.PaymentStatus,
            invoiceNumber = i.InvoiceNumber,
            referenceNumber = i.InvoiceNumber
        });

        return Ok(history);
    }

    [HttpGet("customer-sales-invoices")]
    public async Task<IActionResult> CustomerSalesInvoices()
    {
        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
                .ThenInclude(c => c!.User)
            .Include(i => i.Items)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var result = invoices.Select(i => new
        {
            id = i.Id,
            invoiceNumber = i.InvoiceNumber,
            customerId = i.CustomerId,
            customerName = i.Customer?.User?.Name ?? "",
            items = i.Items.Select(item => new
            {
                partName = item.PartName,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice
            }),
            subtotal = i.TotalAmount,
            discount = i.DiscountAmount,
            discountApplied = i.DiscountApplied,
            grandTotal = i.GrandTotal,
            paymentStatus = i.PaymentStatus,
            paidAmount = i.PaidAmount,
            remainingAmount = i.PaymentStatus == "Credit"
                ? Math.Max(0m, i.GrandTotal - i.PaidAmount)
                : 0m,
            date = i.InvoiceDate.ToString("yyyy-MM-dd"),
            emailSent = i.EmailSent
        });

        return Ok(result);
    }
}
