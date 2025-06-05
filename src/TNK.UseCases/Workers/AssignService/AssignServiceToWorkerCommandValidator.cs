using FluentValidation;
using System;

namespace TNK.UseCases.Workers.AssignService;

public class AssignServiceToWorkerCommandValidator : AbstractValidator<AssignServiceToWorkerCommand>
{
  public AssignServiceToWorkerCommandValidator()
  {
    RuleFor(cmd => cmd.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(cmd => cmd.ServiceId)
        .NotEmpty().WithMessage("ServiceId is required.");

    RuleFor(cmd => cmd.BusinessProfileId)
        .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");
  }
}
