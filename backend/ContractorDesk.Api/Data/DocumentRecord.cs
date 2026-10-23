using ContractorDesk.Api.Models.Documents;

namespace ContractorDesk.Api.Data;

/// <summary>
/// A saved Invoice or Quotation. The summary columns (number, customer, total, timestamps) are
/// denormalized from the payload so the history list can be queried without deserializing documents;
/// <see cref="Payload"/> holds the full <c>InvoiceRequest</c> / <c>QuotationRequest</c> as JSON.
/// </summary>
public class DocumentRecord
{
    public Guid Id { get; set; }
    public DocumentType Type { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
