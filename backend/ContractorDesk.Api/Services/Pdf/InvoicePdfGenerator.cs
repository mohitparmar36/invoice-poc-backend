using ContractorDesk.Api.Models.Common;
using ContractorDesk.Api.Models.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorDesk.Api.Services.Pdf;

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceRequest request, List<LineItemView> items, DocumentTotals totals);
}

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoiceRequest request, List<LineItemView> items, DocumentTotals totals)
    {
        var theme = PdfTheme.Invoice;

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
                            ("Invoice No.", request.InvoiceNumber),
                            ("Invoice Date", request.InvoiceDate.ToString("dd MMM yyyy"))
                        },
                        theme,
                        partyLabel: "BILL TO"));

                    col.Item().PaddingTop(5).Element(c => PdfComponents.ItemsTable(
                        c, items, theme, request.CustomColumns, request.Items.Select(i => (IReadOnlyDictionary<string, string>?)i.CustomValues).ToList()));

                    col.Item().PaddingTop(15).Element(c =>
                    {
                        if (request.GstEnabled)
                        {
                            PdfComponents.Totals(c, totals, theme);
                        }
                        else
                        {
                            PdfComponents.SimpleTotal(c, totals.Subtotal, theme);
                        }
                    });

                    col.Item().Element(c => PdfComponents.NotesAndTerms(c, request.Notes, request.TermsAndConditions));

                    col.Item().Element(c => PdfComponents.BankAndSignature(c, request.Business, showBankDetails: true));
                });

                page.Footer().PaddingHorizontal(30).PaddingBottom(15).Element(c => PdfComponents.Footer(c, theme));
            });
        });

        return document.GeneratePdf();
    }
}
