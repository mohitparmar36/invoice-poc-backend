namespace ContractorDesk.Api.Models.Invoice;

public class InvoiceItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    /// <summary>Values for the document's custom columns, keyed by <c>CustomColumn.Id</c>.</summary>
    public Dictionary<string, string>? CustomValues { get; set; }
}
