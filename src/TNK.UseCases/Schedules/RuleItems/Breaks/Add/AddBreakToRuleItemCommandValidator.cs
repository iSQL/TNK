using FluentValidation;
using System; // For TimeOnly

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Add;

public class AddBreakToRuleItemCommandValidator : AbstractValidator<AddBreakToRuleItemCommand>
{
  public AddBreakToRuleItemCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleRuleItemId)
        .NotEmpty().WithMessage("ScheduleRuleItemId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.BreakName)
        .NotEmpty().WithMessage("Break name is required.")
        .MaximumLength(255).WithMessage("Break name cannot exceed 255 characters.");

    RuleFor(x => x.BreakStartTime)
        .NotEmpty().WithMessage("Break start time is required.");

    RuleFor(x => x.BreakEndTime)
        .NotEmpty().WithMessage("Break end time is required.")
        .GreaterThan(x => x.BreakStartTime)
        .WithMessage("Break end time must be after break start time.");
  }
}
