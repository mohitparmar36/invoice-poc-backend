using System.Globalization;
using System.Text.Json;
using ContractorDesk.Api.Data;
using ContractorDesk.Api.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace ContractorDesk.Api.Services.Documents;

/// <summary>Denormalized summary fields written alongside a document's payload.</summary>
public record DocumentFields(string DocumentNumber, string CustomerName, string ProjectName, decimal Total);

public record HistoryQuery(DocumentType? Type, string? Search, int Skip, int Take);

public interface IDocumentStore
{
    Task<SavedDocument<T>?> GetAsync<T>(DocumentType type, Guid id, CancellationToken ct);

    Task<SavedDocument<T>> CreateAsync<T>(DocumentType type, DocumentFields fields, T payload, CancellationToken ct);

    /// <summary>Updates the existing record in place; the stored document number is never changed. Returns null if not found.</summary>
    Task<SavedDocument<T>?> UpdateAsync<T>(DocumentType type, Guid id, DocumentFields fields, T payload, CancellationToken ct);

    Task<bool> DeleteAsync(DocumentType type, Guid id, CancellationToken ct);

    Task<bool> NumberExistsAsync(DocumentType type, string documentNumber, CancellationToken ct);

    /// <summary>Next free number for <paramref name="prefix"/>, e.g. "INV-2026-" → "INV-2026-004".</summary>
    Task<string> NextNumberAsync(DocumentType type, string prefix, CancellationToken ct);

    Task<HistoryPage> QueryHistoryAsync(HistoryQuery query, CancellationToken ct);
}

public class DocumentStore : IDocumentStore
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public DocumentStore(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SavedDocument<T>?> GetAsync<T>(DocumentType type, Guid id, CancellationToken ct)
    {
        var record = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.Type == type, ct);
        return record is null ? null : ToSavedDocument<T>(record);
    }

    public async Task<SavedDocument<T>> CreateAsync<T>(DocumentType type, DocumentFields fields, T payload, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var record = new DocumentRecord
        {
            Id = Guid.NewGuid(),
            Type = type,
            DocumentNumber = fields.DocumentNumber.Trim(),
            CreatedAt = now,
        };
        Apply(record, fields, payload, now);

        _db.Documents.Add(record);
        await _db.SaveChangesAsync(ct);
        return ToSavedDocument<T>(record);
    }

    public async Task<SavedDocument<T>?> UpdateAsync<T>(DocumentType type, Guid id, DocumentFields fields, T payload, CancellationToken ct)
    {
        var record = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.Type == type, ct);
        if (record is null) return null;

        Apply(record, fields, payload, _clock.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(ct);
        return ToSavedDocument<T>(record);
    }

    public async Task<bool> DeleteAsync(DocumentType type, Guid id, CancellationToken ct)
    {
        var deleted = await _db.Documents.Where(d => d.Id == id && d.Type == type).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public Task<bool> NumberExistsAsync(DocumentType type, string documentNumber, CancellationToken ct)
    {
        // DocumentNumber uses the NOCASE collation, so this comparison is case-insensitive in SQL.
        var normalized = documentNumber.Trim();
        return _db.Documents.AnyAsync(d => d.Type == type && d.DocumentNumber == normalized, ct);
    }

    public async Task<string> NextNumberAsync(DocumentType type, string prefix, CancellationToken ct)
    {
        var pattern = EscapeLike(prefix) + "%";
        var numbers = await _db.Documents.AsNoTracking()
            .Where(d => d.Type == type && EF.Functions.Like(d.DocumentNumber, pattern, "\\"))
            .Select(d => d.DocumentNumber)
            .ToListAsync(ct);

        var max = numbers
            .Select(n => int.TryParse(n[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(max + 1).ToString("D3", CultureInfo.InvariantCulture)}";
    }

    public async Task<HistoryPage> QueryHistoryAsync(HistoryQuery query, CancellationToken ct)
    {
        var documents = _db.Documents.AsNoTracking();

        if (query.Type is { } type)
        {
            documents = documents.Where(d => d.Type == type);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = "%" + EscapeLike(search) + "%";
            // Typing a type name ("inv", "quotation") also matches documents of that type.
            var matchesInvoices = "invoice".Contains(search, StringComparison.OrdinalIgnoreCase);
            var matchesQuotations = "quotation".Contains(search, StringComparison.OrdinalIgnoreCase);

            documents = documents.Where(d =>
                EF.Functions.Like(d.DocumentNumber, pattern, "\\") ||
                EF.Functions.Like(d.CustomerName, pattern, "\\") ||
                EF.Functions.Like(d.ProjectName, pattern, "\\") ||
                (matchesInvoices && d.Type == DocumentType.Invoice) ||
                (matchesQuotations && d.Type == DocumentType.Quotation));
        }

        var totalCount = await documents.CountAsync(ct);

        var items = await documents
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(d => new DocumentSummary
            {
                Id = d.Id,
                Type = d.Type,
                DocumentNumber = d.DocumentNumber,
                CustomerName = d.CustomerName,
                ProjectName = d.ProjectName,
                Total = d.Total,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
            })
            .ToListAsync(ct);

        return new HistoryPage
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = query.Skip + items.Count < totalCount,
        };
    }

    private static void Apply<T>(DocumentRecord record, DocumentFields fields, T payload, DateTime now)
    {
        record.CustomerName = fields.CustomerName.Trim();
        record.ProjectName = fields.ProjectName.Trim();
        record.Total = fields.Total;
        record.Payload = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        record.UpdatedAt = now;
    }

    private static SavedDocument<T> ToSavedDocument<T>(DocumentRecord record) => new()
    {
        Id = record.Id,
        Type = record.Type,
        DocumentNumber = record.DocumentNumber,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        Data = JsonSerializer.Deserialize<T>(record.Payload, PayloadJsonOptions)!,
    };

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
