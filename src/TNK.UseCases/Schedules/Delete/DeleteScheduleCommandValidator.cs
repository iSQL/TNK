using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.Delete;

public class DeleteScheduleCommandValidator : AbstractValidator<DeleteScheduleCommand>
{
  public DeleteScheduleCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for authorization context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
