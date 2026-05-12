using GearNXT_Backend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GearNXT_Backend.Services;

public static class SalesInvoicePdfBuilder
{
    static SalesInvoicePdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(SalesInvoice invoice)
    {
        var customerName = invoice.Customer?.User?.Name ?? "Customer";
        var customerEmail = invoice.Customer?.User?.Email ?? "";
        var items = invoice.Items?.ToList() ?? new List<SalesInvoiceItem>();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("GearNXT").FontSize(22).Bold();
                        col.Item().Text("Vehicle Parts Suite").FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().Text("Kathmandu, Nepal · contact@gearnxt.com").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(200).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Invoice #{invoice.InvoiceNumber}").FontSize(14).Bold();
                        col.Item().AlignRight().Text($"Date: {invoice.InvoiceDate:yyyy-MM-dd}").FontSize(10);
                        col.Item().AlignRight().Text($"Customer: {customerName}").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(customerEmail))
                        {
                            col.Item().AlignRight().Text(customerEmail).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                        col.Item().AlignRight().Text($"Payment: {invoice.PaymentStatus}").FontSize(9);
                    });
                });

                page.Content().Column(column =>
                {
                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(6);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Item").FontSize(10).SemiBold();
                            header.Cell().Element(CellHeader).AlignRight().Text("Qty").FontSize(10).SemiBold();
                            header.Cell().Element(CellHeader).AlignRight().Text("Unit (NPR)").FontSize(10).SemiBold();
                            header.Cell().Element(CellHeader).AlignRight().Text("Total (NPR)").FontSize(10).SemiBold();
                        });

                        if (items.Count == 0)
                        {
                            table.Cell().Element(CellBody).Text("No line items").FontSize(10);
                            table.Cell().Element(CellBody).Text("");
                            table.Cell().Element(CellBody).Text("");
                            table.Cell().Element(CellBody).Text("");
                        }
                        else
                        {
                            foreach (var item in items)
                            {
                                var lineTotal = item.Quantity * item.UnitPrice;
                                table.Cell().Element(CellBody).Text(item.PartName ?? "Item").FontSize(10);
                                table.Cell().Element(CellBody).AlignRight().Text(item.Quantity.ToString()).FontSize(10);
                                table.Cell().Element(CellBody).AlignRight().Text(item.UnitPrice.ToString("F2")).FontSize(10);
                                table.Cell().Element(CellBody).AlignRight().Text(lineTotal.ToString("F2")).FontSize(10);
                            }
                        }
                    });

                    column.Item().PaddingTop(12).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: NPR {invoice.TotalAmount:F2}").FontSize(10);
                        totals.Item().Text(
                            invoice.DiscountApplied
                                ? $"Discount: NPR {invoice.DiscountAmount:F2}"
                                : "Discount: NPR 0.00").FontSize(10);
                        totals.Item().Text($"Paid: NPR {invoice.PaidAmount:F2}").FontSize(10);
                        if (string.Equals(invoice.PaymentStatus, "Credit", StringComparison.OrdinalIgnoreCase))
                        {
                            var remaining = Math.Max(0, invoice.GrandTotal - invoice.PaidAmount);
                            totals.Item().Text($"Remaining: NPR {remaining:F2}").FontSize(10);
                        }
                        totals.Item().PaddingTop(4).Text($"Grand Total: NPR {invoice.GrandTotal:F2}").FontSize(12).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Thank you for your business.").FontSize(9).SemiBold();
                    text.Span("  |  Page ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(9);
                    text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CellHeader(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Background(Colors.Grey.Lighten4);

    private static IContainer CellBody(IContainer container) => container.Padding(6);
}
