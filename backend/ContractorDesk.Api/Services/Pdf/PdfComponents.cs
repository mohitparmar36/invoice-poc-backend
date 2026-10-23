using ContractorDesk.Api.Models.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorDesk.Api.Services.Pdf;

/// <summary>
/// Shared, low-level layout building blocks reused by the Quotation and Invoice
/// PDF generators. Keeping these here means a new document template (or a
/// re-branded layout) can be introduced later without touching this file.
/// </summary>
public static class PdfComponents
{
    public static void Header(IContainer container, BusinessDetails business, PdfTheme theme)
    {
        container.Background(theme.HeaderBackground).Padding(20).Row(row =>
        {
            row.ConstantItem(70).Height(70).AlignMiddle().Element(logoContainer =>
            {
                if (!string.IsNullOrWhiteSpace(business.LogoBase64) && TryDecodeBase64(business.LogoBase64, out var bytes))
                {
                    logoContainer.Image(bytes).FitArea();
                }
                else
                {
                    logoContainer
                        .Background(theme.AccentColor)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text(GetInitials(business.Name))
                        .FontSize(22)
                        .FontColor(Colors.White)
                        .Bold();
                }
            });

            row.RelativeItem().PaddingLeft(15).Column(col =>
            {
                col.Item().Text(string.IsNullOrWhiteSpace(business.Name) ? "Your Business Name" : business.Name)
                    .FontSize(20).Bold().FontColor(theme.AccentDark);

                if (!string.IsNullOrWhiteSpace(business.Address))
                    col.Item().Text(business.Address).FontSize(9).FontColor(Colors.Grey.Darken2);

                col.Item().Row(contactRow =>
                {
                    var contactBits = new List<string>();
                    if (!string.IsNullOrWhiteSpace(business.Phone)) contactBits.Add($"Ph: {business.Phone}");
                    if (!string.IsNullOrWhiteSpace(business.Email)) contactBits.Add(business.Email);
                    if (!string.IsNullOrWhiteSpace(business.Gstin)) contactBits.Add($"GSTIN: {business.Gstin}");
                    contactRow.RelativeItem().Text(string.Join("   •   ", contactBits)).FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });

            row.ConstantItem(150).AlignRight().AlignMiddle().Text(theme.DocumentLabel)
                .FontSize(22).Bold().FontColor(theme.AccentColor);
        });
    }

    public static void MetaAndParty(
        IContainer container,
        CustomerDetails customer,
        string projectName,
        (string Label, string Value)[] metaRows,
        PdfTheme theme,
        string partyLabel = "BILL TO")
    {
        container.PaddingVertical(15).Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
            {
                col.Item().Text(partyLabel).FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(4).Text(string.IsNullOrWhiteSpace(customer.Name) ? "Customer Name" : customer.Name)
                    .FontSize(12).Bold();
                if (!string.IsNullOrWhiteSpace(customer.Address))
                    col.Item().Text(customer.Address).FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(customer.Phone))
                    col.Item().Text($"Ph: {customer.Phone}").FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(customer.Email))
                    col.Item().Text(customer.Email).FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(customer.Gstin))
                    col.Item().Text($"GSTIN: {customer.Gstin}").FontSize(9.5f).FontColor(Colors.Grey.Darken2);

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    col.Item().PaddingTop(6).Text($"Name of Work: {projectName}").FontSize(9.5f).Italic().FontColor(theme.AccentDark);
                }
            });

            row.ConstantItem(15);

            row.ConstantItem(220).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
            {
                foreach (var (label, value) in metaRows)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(label).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(value).FontSize(9.5f).Bold();
                    });
                }
            });
        });
    }

    /// <param name="customColumns">Optional extra text columns, printed after Amount.</param>
    /// <param name="customValues">Per-item values for <paramref name="customColumns"/>, in the same order as <paramref name="items"/>.</param>
    public static void ItemsTable(
        IContainer container,
        List<LineItemView> items,
        PdfTheme theme,
        IReadOnlyList<CustomColumn>? customColumns = null,
        IReadOnlyList<IReadOnlyDictionary<string, string>?>? customValues = null)
    {
        var extraColumns = customColumns ?? Array.Empty<CustomColumn>();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(4);
                columns.ConstantColumn(55);
                columns.ConstantColumn(55);
                columns.ConstantColumn(80);
                columns.ConstantColumn(90);
                foreach (var _ in extraColumns) columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(e => HeaderCell(e, theme)).Text("#");
                header.Cell().Element(e => HeaderCell(e, theme)).Text("Description");
                header.Cell().Element(e => HeaderCell(e, theme)).AlignRight().Text("Qty");
                header.Cell().Element(e => HeaderCell(e, theme)).AlignCenter().Text("Unit");
                header.Cell().Element(e => HeaderCell(e, theme)).AlignRight().Text("Rate");
                header.Cell().Element(e => HeaderCell(e, theme)).AlignRight().Text("Amount");
                foreach (var column in extraColumns)
                    header.Cell().Element(e => HeaderCell(e, theme)).Text(column.Name);
            });

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isEven = i % 2 == 0;

                table.Cell().Element(e => BodyCell(e, isEven)).Text((i + 1).ToString());
                table.Cell().Element(e => BodyCell(e, isEven)).Text(item.Description);
                table.Cell().Element(e => BodyCell(e, isEven)).AlignRight().Text(FormatQty(item.Quantity));
                table.Cell().Element(e => BodyCell(e, isEven)).AlignCenter().Text(item.Unit);
                table.Cell().Element(e => BodyCell(e, isEven)).AlignRight().Text(IndianCurrencyFormatter.Format(item.Rate));
                table.Cell().Element(e => BodyCell(e, isEven)).AlignRight().Text(IndianCurrencyFormatter.Format(item.Amount)).Bold();
                var values = customValues is not null && i < customValues.Count ? customValues[i] : null;
                foreach (var column in extraColumns)
                    table.Cell().Element(e => BodyCell(e, isEven)).Text(values?.GetValueOrDefault(column.Id) ?? string.Empty);
            }
        });

        static IContainer HeaderCell(IContainer c, PdfTheme theme) => c
            .Background(theme.TableHeaderBackground)
            .PaddingVertical(8).PaddingHorizontal(6)
            .DefaultTextStyle(t => t.FontColor(Colors.White).FontSize(9.5f).Bold());

        static IContainer BodyCell(IContainer c, bool isEven) => c
            .Background(isEven ? Colors.White : Colors.Grey.Lighten5)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(7).PaddingHorizontal(6)
            .DefaultTextStyle(t => t.FontSize(9.5f));
    }

    public static void Totals(IContainer container, DocumentTotals totals, PdfTheme theme)
    {
        container.AlignRight().Width(260).Column(col =>
        {
            TotalsRow(col, "Subtotal", IndianCurrencyFormatter.Format(totals.Subtotal));
            TotalsRow(col, $"CGST ({totals.CgstPercent:0.##}%)", IndianCurrencyFormatter.Format(totals.CgstAmount));
            TotalsRow(col, $"SGST ({totals.SgstPercent:0.##}%)", IndianCurrencyFormatter.Format(totals.SgstAmount));

            col.Item().PaddingTop(6).BorderTop(1).BorderColor(theme.AccentColor).PaddingTop(6).Background(theme.HeaderBackground).Padding(8).Row(r =>
            {
                r.RelativeItem().Text("Grand Total").FontSize(12).Bold().FontColor(theme.AccentDark);
                r.RelativeItem().AlignRight().Text(IndianCurrencyFormatter.Format(totals.GrandTotal)).FontSize(13).Bold().FontColor(theme.AccentDark);
            });
        });

        static void TotalsRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingVertical(2).Row(r =>
            {
                r.RelativeItem().Text(label).FontSize(10).FontColor(Colors.Grey.Darken2);
                r.RelativeItem().AlignRight().Text(value).FontSize(10);
            });
        }
    }

    public static void SimpleTotal(IContainer container, decimal total, PdfTheme theme)
    {
        container.AlignRight().Width(260).Background(theme.HeaderBackground).Padding(8).Row(r =>
        {
            r.RelativeItem().Text("Total Amount").FontSize(12).Bold().FontColor(theme.AccentDark);
            r.RelativeItem().AlignRight().Text(IndianCurrencyFormatter.Format(total)).FontSize(13).Bold().FontColor(theme.AccentDark);
        });
    }

    public static void NotesAndTerms(IContainer container, string? notes, string? terms)
    {
        container.Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(notes))
            {
                col.Item().PaddingTop(10).Text("Notes").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken2);
                col.Item().Text(notes).FontSize(9).FontColor(Colors.Grey.Darken2);
            }

            if (!string.IsNullOrWhiteSpace(terms))
            {
                col.Item().PaddingTop(10).Text("Terms & Conditions").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken2);
                col.Item().Text(terms).FontSize(9).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    public static void BankAndSignature(IContainer container, BusinessDetails business, bool showBankDetails = true)
    {
        container.PaddingTop(20).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                var hasBankDetails = showBankDetails && (!string.IsNullOrWhiteSpace(business.BankName) || !string.IsNullOrWhiteSpace(business.AccountNumber));
                if (hasBankDetails)
                {
                    col.Item().Text("Bank Details").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(business.BankName))
                        col.Item().Text($"Bank: {business.BankName}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(business.AccountName))
                        col.Item().Text($"A/C Name: {business.AccountName}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(business.AccountNumber))
                        col.Item().Text($"A/C No: {business.AccountNumber}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(business.Ifsc))
                        col.Item().Text($"IFSC: {business.Ifsc}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(business.Branch))
                        col.Item().Text($"Branch: {business.Branch}").FontSize(9).FontColor(Colors.Grey.Darken2);
                }
            });

            row.ConstantItem(200).Column(col =>
            {
                col.Item().Height(50);
                col.Item().BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(4)
                    .AlignCenter().Text($"For {(string.IsNullOrWhiteSpace(business.Name) ? "the Business" : business.Name)}").FontSize(9);
                col.Item().AlignCenter().Text("Authorized Signatory").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    public static void Footer(IContainer container, PdfTheme theme)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Medium));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private static string FormatQty(decimal qty) => qty % 1 == 0 ? qty.ToString("0") : qty.ToString("0.##");

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "CD";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant() : name[..Math.Min(2, name.Length)].ToUpperInvariant();
    }

    private static bool TryDecodeBase64(string base64, out byte[] bytes)
    {
        try
        {
            var commaIndex = base64.IndexOf(',');
            var data = commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
            bytes = Convert.FromBase64String(data);
            return true;
        }
        catch
        {
            bytes = [];
            return false;
        }
    }
}
