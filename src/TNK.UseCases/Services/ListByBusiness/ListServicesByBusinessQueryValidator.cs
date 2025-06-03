using FluentValidation;
using TNK.UseCases.Services.ListByBusiness; // Ensure this matches the namespace of your Query

namespace TNK.UseCases.Services.ListByBusiness;

public class ListServicesByBusinessQueryValidator : AbstractValidator<ListServicesByBusinessQuery>
{
  public ListServicesByBusinessQueryValidator()
  {
    // Example: Assuming ListServicesByBusinessQuery has a BusinessProfileId property
    RuleFor(query => query.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

    // Add other rules as needed, for example, if you have pagination parameters:
    // RuleFor(query => query.PageNumber).GreaterThan(0);
    // RuleFor(query => query.PageSize).GreaterThan(0);
  }
}
