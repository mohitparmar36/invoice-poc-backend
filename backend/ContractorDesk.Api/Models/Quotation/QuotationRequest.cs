using ContractorDesk.Api.Models.Common;

namespace ContractorDesk.Api.Models.Quotation;

public class QuotationRequest
{
    public BusinessDetails Business { get; set; } = new();
    public CustomerDetails Customer { get; set; } = new();

    public string QuotationNumber { get; set; } = string.Empty;
    public DateOnly QuotationDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public List<QuotationItem> Items { get; set; } = new();

    /// <summary>Optional extra line-item columns; null or empty means the standard table.</summary>
    public List<CustomColumn>? CustomColumns { get; set; }

    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
}
