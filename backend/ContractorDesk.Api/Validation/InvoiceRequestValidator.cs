using ContractorDesk.Api.Models.Invoice;
using ContractorDesk.Api.Models.Common;
using FluentValidation;

namespace ContractorDesk.Api.Validation;

public class InvoiceItemValidator : AbstractValidator<InvoiceItem>
{
    public InvoiceItemValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("Item description is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative.");
    }
}

public class InvoiceRequestValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceRequestValidator()
    {
        RuleFor(x => x.Business).SetValidator(new BusinessDetailsValidator());
        RuleFor(x => x.Customer).SetValidator(new CustomerDetailsValidator());

        RuleFor(x => x.InvoiceNumber).NotEmpty().WithMessage("Invoice number is required.");
        RuleFor(x => x.InvoiceDate).NotEqual(default(DateOnly)).WithMessage("Invoice date is required.");

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Items).SetValidator(new InvoiceItemValidator());

        RuleFor(x => x.CustomColumns!.Count).LessThanOrEqualTo(CustomColumn.MaxPerDocument)
            .When(x => x.CustomColumns is not null)
            .WithName("CustomColumns")
            .WithMessage($"At most {CustomColumn.MaxPerDocument} extra columns are allowed.");
        RuleForEach(x => x.CustomColumns).SetValidator(new CustomColumnValidator());

        RuleFor(x => x.GstPercent).GreaterThanOrEqualTo(0).WithMessage("GST cannot be negative.");
    }
}
