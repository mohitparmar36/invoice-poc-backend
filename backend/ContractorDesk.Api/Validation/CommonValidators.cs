using ContractorDesk.Api.Models.Common;
using FluentValidation;

namespace ContractorDesk.Api.Validation;

public class BusinessDetailsValidator : AbstractValidator<BusinessDetails>
{
    public BusinessDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Business name is required.");
    }
}

public class CustomColumnValidator : AbstractValidator<CustomColumn>
{
    public CustomColumnValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Column id is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Each extra column needs a name.")
            .MaximumLength(CustomColumn.MaxNameLength).WithMessage($"Column names can be at most {CustomColumn.MaxNameLength} characters.");
    }
}

public class CustomerDetailsValidator : AbstractValidator<CustomerDetails>
{
    public CustomerDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Customer name is required.");
    }
}
