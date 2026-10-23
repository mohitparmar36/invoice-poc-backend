using ContractorDesk.Api.Models.Documents;
using ContractorDesk.Api.Models.Quotation;
using ContractorDesk.Api.Services.Calculations;
using ContractorDesk.Api.Services.Documents;
using ContractorDesk.Api.Services.Pdf;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorDesk.Api.Controllers;

[ApiController]
[Route("api/quotations")]
public class QuotationsController : ControllerBase
{
    private readonly IValidator<QuotationRequest> _validator;
    private readonly ITotalsCalculator _totalsCalculator;
    private readonly IQuotationPdfGenerator _pdfGenerator;
    private readonly IDocumentStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<QuotationsController> _logger;

    public QuotationsController(
        IValidator<QuotationRequest> validator,
        ITotalsCalculator totalsCalculator,
        IQuotationPdfGenerator pdfGenerator,
        IDocumentStore store,
        TimeProvider clock,
        ILogger<QuotationsController> logger)
    {
        _validator = validator;
        _totalsCalculator = totalsCalculator;
        _pdfGenerator = pdfGenerator;
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    [HttpPost("pdf")]
    public IActionResult GeneratePdf([FromBody] QuotationRequest request)
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

            var totalAmount = Math.Round(lineItems.Sum(i => i.Amount), 2, MidpointRounding.AwayFromZero);

            var pdfBytes = _pdfGenerator.Generate(request, lineItems, totalAmount);

            var fileName = $"Quotation-{SanitizeFileName(request.QuotationNumber)}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quotation PDF");
            return Problem("An error occurred while generating the quotation PDF.", statusCode: 500);
        }
    }

    [HttpGet("next-number")]
    public async Task<ActionResult<NextNumberResponse>> GetNextNumber(CancellationToken ct)
    {
        return new NextNumberResponse { DocumentNumber = await _store.NextNumberAsync(DocumentType.Quotation, NumberPrefix(), ct) };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedDocument<QuotationRequest>>> Get(Guid id, CancellationToken ct)
    {
        var saved = await _store.GetAsync<QuotationRequest>(DocumentType.Quotation, id, ct);
        return saved is null ? NotFoundProblem() : saved;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuotationRequest request, CancellationToken ct)
    {
        request.QuotationNumber = request.QuotationNumber.Trim();

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult);
        }

        if (await _store.NumberExistsAsync(DocumentType.Quotation, request.QuotationNumber, ct))
        {
            return DuplicateNumber(request.QuotationNumber);
        }

        try
        {
            var saved = await _store.CreateAsync(DocumentType.Quotation, ToFields(request), request, ct);
            return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
        }
        catch (DbUpdateException)
        {
            // Lost a race with a concurrent create using the same number; the unique index caught it.
            if (await _store.NumberExistsAsync(DocumentType.Quotation, request.QuotationNumber, ct))
            {
                return DuplicateNumber(request.QuotationNumber);
            }
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuotationRequest request, CancellationToken ct)
    {
        var existing = await _store.GetAsync<QuotationRequest>(DocumentType.Quotation, id, ct);
        if (existing is null)
        {
            return NotFoundProblem();
        }

        // The quotation number is fixed once the quotation is created.
        request.QuotationNumber = existing.DocumentNumber;

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationFailure(validationResult);
        }

        var saved = await _store.UpdateAsync(DocumentType.Quotation, id, ToFields(request), request, ct);
        return saved is null ? NotFoundProblem() : Ok(saved);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _store.DeleteAsync(DocumentType.Quotation, id, ct) ? NoContent() : NotFoundProblem();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var source = await _store.GetAsync<QuotationRequest>(DocumentType.Quotation, id, ct);
        if (source is null)
        {
            return NotFoundProblem();
        }

        var copy = source.Data;
        copy.QuotationNumber = await _store.NextNumberAsync(DocumentType.Quotation, NumberPrefix(), ct);
        copy.QuotationDate = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

        var saved = await _store.CreateAsync(DocumentType.Quotation, ToFields(copy), copy, ct);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, saved);
    }

    private DocumentFields ToFields(QuotationRequest request)
    {
        var lineItems = _totalsCalculator.BuildLineItems(
            request.Items.Select(i => (i.Description, i.Quantity, i.Unit, i.Rate)));
        var totalAmount = Math.Round(lineItems.Sum(i => i.Amount), 2, MidpointRounding.AwayFromZero);

        return new DocumentFields(request.QuotationNumber, request.Customer.Name, request.ProjectName, totalAmount);
    }

    private string NumberPrefix() => $"QUO-{_clock.GetLocalNow().Year}-";

    private IActionResult ValidationFailure(ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return ValidationProblem(new ValidationProblemDetails(errors));
    }

    private ObjectResult DuplicateNumber(string quotationNumber)
    {
        var message = $"A quotation with number {quotationNumber} already exists. Please use a different quotation number.";
        return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [nameof(QuotationRequest.QuotationNumber)] = new[] { message },
        })
        {
            Title = message,
            Status = StatusCodes.Status409Conflict,
        });
    }

    private ObjectResult NotFoundProblem() =>
        Problem(title: "This quotation could not be found. It may have been deleted.", statusCode: StatusCodes.Status404NotFound);

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "document";
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Where(c => !invalidChars.Contains(c)).ToArray());
    }
}
