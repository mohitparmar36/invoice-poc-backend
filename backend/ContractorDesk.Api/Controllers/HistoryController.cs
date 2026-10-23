using ContractorDesk.Api.Models.Documents;
using ContractorDesk.Api.Services.Documents;
using Microsoft.AspNetCore.Mvc;

namespace ContractorDesk.Api.Controllers;

/// <summary>Lightweight, paginated list of saved invoices and quotations, most recently updated first.</summary>
[ApiController]
[Route("api/history")]
public class HistoryController : ControllerBase
{
    private const int DefaultPageSize = 30;
    private const int MaxPageSize = 100;

    private readonly IDocumentStore _store;

    public HistoryController(IDocumentStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<HistoryPage>> Get(
        [FromQuery] DocumentType? type,
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = DefaultPageSize,
        CancellationToken ct = default)
    {
        var query = new HistoryQuery(type, search, Math.Max(0, skip), Math.Clamp(take, 1, MaxPageSize));
        return await _store.QueryHistoryAsync(query, ct);
    }
}
