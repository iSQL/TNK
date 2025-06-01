using FluentValidation;

namespace TNK.UseCases.Services.Delete;

public class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
{
  public DeleteServiceCommandValidator()
  {
    RuleFor(x => x.ServiceId)
        .NotEmpty().WithMessage("ServiceId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
