using ContractorDesk.Api.Models.Quotation;
using ContractorDesk.Api.Models.Common;
using FluentValidation;

namespace ContractorDesk.Api.Validation;

public class MeasurementRowValidator : AbstractValidator<MeasurementRow>
{
    public MeasurementRowValidator()
    {
        RuleFor(x => x.No).GreaterThanOrEqualTo(0).WithMessage("No. cannot be negative.");
        RuleFor(x => x.Length).GreaterThanOrEqualTo(0).WithMessage("Length cannot be negative.");
        RuleFor(x => x.Width).GreaterThanOrEqualTo(0).WithMessage("Width cannot be negative.");
        RuleFor(x => x.Height).GreaterThanOrEqualTo(0).When(x => x.Height.HasValue).WithMessage("Height cannot be negative.");
    }
}

public class QuotationItemValidator : AbstractValidator<QuotationItem>
{
    public QuotationItemValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("Item description is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative.");
        RuleForEach(x => x.Measurements).SetValidator(new MeasurementRowValidator());
        RuleFor(x => x.ConversionFactor).GreaterThanOrEqualTo(0).When(x => x.ConversionFactor.HasValue)
            .WithMessage("Conversion factor cannot be negative.");
    }
}

public class QuotationRequestValidator : AbstractValidator<QuotationRequest>
{
    public QuotationRequestValidator()
    {
        RuleFor(x => x.Business).SetValidator(new BusinessDetailsValidator());
        RuleFor(x => x.Customer).SetValidator(new CustomerDetailsValidator());

        RuleFor(x => x.QuotationNumber).NotEmpty().WithMessage("Quotation number is required.");
        RuleFor(x => x.QuotationDate).NotEqual(default(DateOnly)).WithMessage("Quotation date is required.");

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Items).SetValidator(new QuotationItemValidator());

        RuleFor(x => x.CustomColumns!.Count).LessThanOrEqualTo(CustomColumn.MaxPerDocument)
            .When(x => x.CustomColumns is not null)
            .WithName("CustomColumns")
            .WithMessage($"At most {CustomColumn.MaxPerDocument} extra columns are allowed.");
        RuleForEach(x => x.CustomColumns).SetValidator(new CustomColumnValidator());
    }
}
