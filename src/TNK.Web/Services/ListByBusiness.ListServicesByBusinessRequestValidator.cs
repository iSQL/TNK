using FluentValidation;
using TNK.Web.Services.ListByBusiness; // Ensure this using points to your request DTO

namespace TNK.Web.Services.Validators;

/// <summary>
/// Validator for the ListServicesByBusinessRequest.
/// </summary>
public class ListServicesByBusinessRequestValidator : Validator<ListServicesByBusinessRequest>
{
  public ListServicesByBusinessRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    // Add rules for pagination parameters if they are added to the request DTO
    // Example:
    // RuleFor(x => x.PageNumber)
    //   .GreaterThan(0).When(x => x.PageNumber.HasValue)
    //   .WithMessage("PageNumber must be greater than 0.");
    //
    // RuleFor(x => x.PageSize)
    //   .GreaterThan(0).When(x => x.PageSize.HasValue)
    //   .WithMessage("PageSize must be greater than 0.");
  }
}
