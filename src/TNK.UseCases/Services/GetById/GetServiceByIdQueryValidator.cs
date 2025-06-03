using FluentValidation;
using TNK.UseCases.Services.GetById; // Ensure this matches the namespace of your Query

namespace TNK.UseCases.Services.GetById;

public class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
  public GetServiceByIdQueryValidator()
  {
    // Example: Assuming GetServiceByIdQuery has ServiceId and BusinessProfileId properties
    RuleFor(query => query.ServiceId)
        .NotEmpty().WithMessage("ServiceId is required.");

    RuleFor(query => query.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
