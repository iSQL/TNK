using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Remove;

public class RemoveBreakRuleCommandValidator : AbstractValidator<RemoveBreakRuleCommand>
{
  public RemoveBreakRuleCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleRuleItemId)
        .NotEmpty().WithMessage("ScheduleRuleItemId is required.");

    RuleFor(x => x.BreakRuleId)
        .NotEmpty().WithMessage("BreakRuleId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
