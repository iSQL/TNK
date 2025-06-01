using FluentValidation;

namespace TNK.UseCases.Services.Update;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
  public UpdateServiceCommandValidator()
  {
    RuleFor(x => x.ServiceId)
        .NotEmpty().WithMessage("ServiceId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.Name)
        .NotEmpty().WithMessage("Service name is required.")
        .MaximumLength(255).WithMessage("Service name cannot exceed 255 characters.");

    RuleFor(x => x.Description)
        .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

    RuleFor(x => x.DurationInMinutes)
        .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes.");

    RuleFor(x => x.Price)
        .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

    RuleFor(x => x.ImageUrl)
        .MaximumLength(2048).WithMessage("Image URL cannot exceed 2048 characters.")
        .Matches(@"^(https?://)?([\da-z.-]+)\.([a-z.]{2,6})([/\w .-]*)*/?$")
        .When(x => !string.IsNullOrEmpty(x.ImageUrl)).WithMessage("Invalid Image URL format.");
  }
}
