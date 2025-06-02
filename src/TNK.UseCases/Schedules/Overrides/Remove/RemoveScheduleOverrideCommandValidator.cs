using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.Overrides.Remove;

public class RemoveScheduleOverrideCommandValidator : AbstractValidator<RemoveScheduleOverrideCommand>
{
  public RemoveScheduleOverrideCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleOverrideId)
        .NotEmpty().WithMessage("ScheduleOverrideId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
