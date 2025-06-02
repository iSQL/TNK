using FluentValidation;

namespace TNK.UseCases.Workers.Create;

public class CreateWorkerCommandValidator : AbstractValidator<CreateWorkerCommand>
{
  public CreateWorkerCommandValidator()
  {
    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

    RuleFor(x => x.FirstName)
        .NotEmpty().WithMessage("First name is required.")
        .MaximumLength(255).WithMessage("First name cannot exceed 255 characters.");

    RuleFor(x => x.LastName)
        .NotEmpty().WithMessage("Last name is required.")
        .MaximumLength(255).WithMessage("Last name cannot exceed 255 characters.");

    RuleFor(x => x.Email)
        .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
        .WithMessage("Invalid email format.")
        .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

    RuleFor(x => x.PhoneNumber)
        .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");
    // Add more specific phone validation if needed

    RuleFor(x => x.ImageUrl)
        .MaximumLength(2048).WithMessage("Image URL cannot exceed 2048 characters.")
        .Matches(@"^(https?://)?([\da-z.-]+)\.([a-z.]{2,6})([/\w .-]*)*/?$")
        .When(x => !string.IsNullOrEmpty(x.ImageUrl)).WithMessage("Invalid Image URL format.");

    RuleFor(x => x.Specialization)
        .MaximumLength(255).WithMessage("Specialization cannot exceed 255 characters.");

    RuleFor(x => x.ApplicationUserId)
        .MaximumLength(450) // Standard max length for ASP.NET Core Identity User Ids (string)
        .When(x => !string.IsNullOrEmpty(x.ApplicationUserId));
  }
}
