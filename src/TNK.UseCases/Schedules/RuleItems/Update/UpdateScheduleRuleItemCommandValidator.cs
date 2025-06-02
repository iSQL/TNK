using FluentValidation;
using System; // For TimeOnly

namespace TNK.UseCases.Schedules.RuleItems.Update;

public class UpdateScheduleRuleItemCommandValidator : AbstractValidator<UpdateScheduleRuleItemCommand>
{
  public UpdateScheduleRuleItemCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleRuleItemId)
        .NotEmpty().WithMessage("ScheduleRuleItemId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.StartTime)
        .NotEmpty().WithMessage("Start time is required.");

    RuleFor(x => x.EndTime)
        .NotEmpty().WithMessage("End time is required.")
        .GreaterThan(x => x.StartTime)
        .When(x => x.IsWorkingDay) // Only if it's a working day
        .WithMessage("End time must be after start time for a working day.");
  }
}
