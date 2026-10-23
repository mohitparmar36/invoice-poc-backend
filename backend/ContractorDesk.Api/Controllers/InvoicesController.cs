using ContractorDesk.Api.Models.Documents;
using ContractorDesk.Api.Models.Invoice;
using ContractorDesk.Api.Services.Calculations;
using ContractorDesk.Api.Services.Documents;
using ContractorDesk.Api.Services.Pdf;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorDesk.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IValidator<InvoiceRequest> _validator;
    private readonly ITotalsCalculator _totalsCalculator;
    private readonly IInvoicePdfGenerator _pdfGenerator;
    private readonly IDocumentStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IValidator<InvoiceRequest> validator,
        ITotalsCalculator totalsCalculator,
        IInvoicePdfGenerator pdfGenerator,
        IDocumentStore store,
        TimeProvider clock,
        ILogger<InvoicesController> logger)
    {
        _validator = validator;
        _totalsCalculator = totalsCalculator;
        _pdfGenerator = pdfGenerator;
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    [HttpPost("pdf")]
    public IActionResult GeneratePdf([FromBody] InvoiceRequest request)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult);
        }

        try
        {
            var lineItems = _totalsCalculator.BuildLineItems(
                request.Items.Select(i => (i.Description, i.Quantity, i.Unit, i.Rate)));

            var effectiveGstPercent = request.GstEnabled ? request.GstPercent : 0m;
            var totals = _totalsCalculator.Calculate(lineItems, effectiveGstPercent);

            var pdfBytes = _pdfGenerator.Generate(request, lineItems, totals);

            var fileName = $"Invoice-{SanitizeFileName(request.InvoiceNumber)}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice PDF");
            return Problem("An error occurred while generating the invoice PDF.", statusCode: 500);
        }
    }

    [HttpGet("next-number")]
    public async Task<ActionResult<NextNumberResponse>> GetNextNumber(CancellationToken ct)
    {
        return new NextNumberResponse { DocumentNumber = await _store.NextNumberAsync(DocumentType.Invoice, NumberPrefix(), ct) };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedDocument<InvoiceRequest>>> Get(Guid id, CancellationToken ct)
    {
        var saved = await _store.GetAsync<InvoiceRequest>(DocumentType.Invoice, id, ct);
        return saved is null ? NotFoundProblem() : saved;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InvoiceRequest request, CancellationToken ct)
    {
        request.InvoiceNumber = request.InvoiceNumber.Trim();

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult);
        }

        if (await _store.NumberExistsAsync(DocumentType.Invoice, request.InvoiceNumber, ct))
        {
            return DuplicateNumber(request.InvoiceNumber);
        }

        try
        {
            var saved = await _store.CreateAsync(DocumentType.Invoice, ToFields(request), request, ct);
            return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
        }
        catch (DbUpdateException)
        {
            // Lost a race with a concurrent create using the same number; the unique index caught it.
            if (await _store.NumberExistsAsync(DocumentType.Invoice, request.InvoiceNumber, ct))
            {
                return DuplicateNumber(request.InvoiceNumber);
            }
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InvoiceRequest request, CancellationToken ct)
    {
        var existing = await _store.GetAsync<InvoiceRequest>(DocumentType.Invoice, id, ct);
        if (existing is null)
        {
            return NotFoundProblem();
        }

        // The invoice number is fixed once the invoice is created.
        request.InvoiceNumber = existing.DocumentNumber;

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult);
        }

        var saved = await _store.UpdateAsync(DocumentType.Invoice, id, ToFields(request), request, ct);
        return saved is null ? NotFoundProblem() : Ok(saved);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _store.DeleteAsync(DocumentType.Invoice, id, ct) ? NoContent() : NotFoundProblem();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var source = await _store.GetAsync<InvoiceRequest>(DocumentType.Invoice, id, ct);
        if (source is null)
        {
            return NotFoundProblem();
        }

        var copy = source.Data;
        copy.InvoiceNumber = await _store.NextNumberAsync(DocumentType.Invoice, NumberPrefix(), ct);
        copy.InvoiceDate = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

        var saved = await _store.CreateAsync(DocumentType.Invoice, ToFields(copy), copy, ct);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    private DocumentFields ToFields(InvoiceRequest request)
    {
        var lineItems = _totalsCalculator.BuildLineItems(
            request.Items.Select(i => (i.Description, i.Quantity, i.Unit, i.Rate)));
        var effectiveGstPercent = request.GstEnabled ? request.GstPercent : 0m;
        var totals = _totalsCalculator.Calculate(lineItems, effectiveGstPercent);

        return new DocumentFields(request.InvoiceNumber, request.Customer.Name, request.ProjectName, totals.GrandTotal);
    }

    private string NumberPrefix() => $"INV-{_clock.GetLocalNow().Year}-";

    private IActionResult ValidationFailure(ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return ValidationProblem(new ValidationProblemDetails(errors));
    }

    private ObjectResult DuplicateNumber(string invoiceNumber)
    {
        var message = $"An invoice with number {invoiceNumber} already exists. Please use a different invoice number.";
        return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [nameof(InvoiceRequest.InvoiceNumber)] = new[] { message },
        })
        {
            Title = message,
            Status = StatusCodes.Status409Conflict,
        });
    }

    private ObjectResult NotFoundProblem() =>
        Problem(title: "This invoice could not be found. It may have been deleted.", statusCode: StatusCodes.Status404NotFound);

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "document";
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Where(c => !invalidChars.Contains(c)).ToArray());
    }
}
