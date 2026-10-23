using ContractorDesk.Api.Models.Common;

namespace ContractorDesk.Api.Models.Invoice;

public class InvoiceRequest
{
    public BusinessDetails Business { get; set; } = new();
    public CustomerDetails Customer { get; set; } = new();

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public List<InvoiceItem> Items { get; set; } = new();

    /// <summary>Optional extra line-item columns; null or empty means the standard table.</summary>
    public List<CustomColumn>? CustomColumns { get; set; }

    public bool GstEnabled { get; set; } = true;
    public decimal GstPercent { get; set; }

    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
}
