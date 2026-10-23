using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorDesk.Api.Services.Pdf;

public class PdfTheme
{
    public required string DocumentLabel { get; init; }
    public required Color AccentColor { get; init; }
    public required Color AccentDark { get; init; }
    public required Color HeaderBackground { get; init; }
    public required Color TableHeaderBackground { get; init; }

    public static readonly PdfTheme Quotation = new()
    {
        DocumentLabel = "QUOTATION",
        AccentColor = Colors.Blue.Medium,
        AccentDark = Colors.Blue.Darken3,
        HeaderBackground = Colors.Blue.Lighten5,
        TableHeaderBackground = Colors.Blue.Darken2
    };

    public static readonly PdfTheme Invoice = new()
    {
        DocumentLabel = "BILL",
        AccentColor = Colors.Teal.Medium,
        AccentDark = Colors.Teal.Darken4,
        HeaderBackground = Colors.Teal.Lighten5,
        TableHeaderBackground = Colors.Teal.Darken2
    };
}
