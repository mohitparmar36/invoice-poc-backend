namespace ContractorDesk.Api.Models.Documents;

/// <summary>Lightweight history row — never includes line items or the full payload.</summary>
public class DocumentSummary
{
    public Guid Id { get; set; }
    public DocumentType Type { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>A full saved document: identity + timestamps, plus the original request model as <see cref="Data"/>.</summary>
public class SavedDocument<T>
{
    public Guid Id { get; set; }
    public DocumentType Type { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public T Data { get; set; } = default!;
}

public class HistoryPage
{
    public List<DocumentSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class NextNumberResponse
{
    public string DocumentNumber { get; set; } = string.Empty;
}
