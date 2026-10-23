using ContractorDesk.Api.Models.Common;
using ContractorDesk.Api.Models.Quotation;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorDesk.Api.Services.Pdf;

public interface IQuotationPdfGenerator
{
    byte[] Generate(QuotationRequest request, List<LineItemView> items, decimal totalAmount);
}

public class QuotationPdfGenerator : IQuotationPdfGenerator
{
    public byte[] Generate(QuotationRequest request, List<LineItemView> items, decimal totalAmount)
    {
        var theme = PdfTheme.Quotation;

        var itemsWithMeasurements = request.Items
            .Where(i => i.Measurements is { Count: > 0 })
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Calibri));

                page.Header().Element(c => PdfComponents.Header(c, request.Business, theme));

                page.Content().PaddingHorizontal(30).Column(col =>
                {
                    col.Item().Element(c => PdfComponents.MetaAndParty(
                        c,
                        request.Customer,
                        request.ProjectName,
                        new[]
                        {
                            ("Quotation No.", request.QuotationNumber),
                            ("Date", request.QuotationDate.ToString("dd MMM yyyy"))
                        },
                        theme,
                        partyLabel: "TO"));

                    col.Item().PaddingTop(5).Element(c => PdfComponents.ItemsTable(
                        c, items, theme, request.CustomColumns, request.Items.Select(i => (IReadOnlyDictionary<string, string>?)i.CustomValues).ToList()));

                    col.Item().PaddingTop(15).Element(c => PdfComponents.SimpleTotal(c, totalAmount, theme));

                    col.Item().Element(c => PdfComponents.NotesAndTerms(c, request.Notes, request.TermsAndConditions));

                    col.Item().Element(c => PdfComponents.BankAndSignature(c, request.Business, showBankDetails: false));
                });

                page.Footer().PaddingHorizontal(30).PaddingBottom(15).Element(c => PdfComponents.Footer(c, theme));
            });

            if (itemsWithMeasurements.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(t => t.FontFamily(Fonts.Calibri));

                    page.Header().Element(c => PdfComponents.Header(c, request.Business, theme));

                    page.Content().PaddingHorizontal(30).PaddingTop(15)
                        .Element(c => MeasurementSheet(c, request, itemsWithMeasurements, theme));

                    page.Footer().PaddingHorizontal(30).PaddingBottom(15).Element(c => PdfComponents.Footer(c, theme));
                });
            }
        });

        return document.GeneratePdf();
    }

    private static void MeasurementSheet(IContainer container, QuotationRequest request, List<QuotationItem> items, PdfTheme theme)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("MEASUREMENT SHEET").FontSize(16).Bold().FontColor(theme.AccentDark);
            col.Item().AlignCenter().PaddingTop(2).Text($"Quotation No. {request.QuotationNumber}").FontSize(9.5f).FontColor(Colors.Grey.Darken1);

            var itemNumber = 1;
            foreach (var item in items)
            {
                var measurements = item.Measurements!;
                var runningTotal = Round(measurements.Sum(RowAmount));
                var conversionFactor = item.ConversionFactor;
                var finalTotal = conversionFactor.HasValue ? Round(runningTotal * conversionFactor.Value) : runningTotal;
                var rowUnitSuffix = string.IsNullOrWhiteSpace(item.MeasurementUnit) ? string.Empty : $" {item.MeasurementUnit}";

                col.Item().PaddingTop(20).Text($"{itemNumber}. {item.Description}").Bold().FontSize(11).FontColor(theme.AccentDark);

                col.Item().PaddingTop(6).PaddingLeft(16).Column(inner =>
                {
                    foreach (var row in measurements)
                    {
                        var amount = Round(RowAmount(row));
                        var factors = $"{FormatNo(row.No)} x {FormatDim(row.Length)} x {FormatDim(row.Width)}"
                            + (row.Height.HasValue ? $" x {FormatDim(row.Height.Value)}" : string.Empty);
                        var calc = $"{factors} = {FormatDim(amount)}{rowUnitSuffix}";
                        var text = string.IsNullOrWhiteSpace(row.Name) ? calc : $"{row.Name} — {calc}";
                        inner.Item().PaddingVertical(2).Text(text).FontSize(10).FontColor(Colors.Grey.Darken3);
                    }

                    var totalLine = conversionFactor.HasValue
                        ? $"= {FormatDim(runningTotal)}{rowUnitSuffix} x {FormatDim(conversionFactor.Value)} = {FormatDim(finalTotal)} {item.Unit}"
                        : $"= {FormatDim(finalTotal)} {item.Unit}";

                    inner.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(4)
                        .Text(totalLine).Bold().FontSize(11).FontColor(theme.AccentDark);
                });

                itemNumber++;
            }
        });
    }

    private static decimal RowAmount(MeasurementRow row) => row.No * row.Length * row.Width * (row.Height ?? 1m);

    private static string FormatNo(decimal value) => value % 1 == 0 ? value.ToString("0") : value.ToString("0.##");

    private static string FormatDim(decimal value) => value.ToString("0.00");

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
