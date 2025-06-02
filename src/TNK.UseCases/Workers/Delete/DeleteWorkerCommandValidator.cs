using FluentValidation;

namespace TNK.UseCases.Workers.Delete;

public class DeleteWorkerCommandValidator : AbstractValidator<DeleteWorkerCommand>
{
  public DeleteWorkerCommandValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
