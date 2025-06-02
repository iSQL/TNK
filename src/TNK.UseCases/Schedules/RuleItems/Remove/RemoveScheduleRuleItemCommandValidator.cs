using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Schedules.RuleItems.Remove;

public class RemoveScheduleRuleItemCommandValidator : AbstractValidator<RemoveScheduleRuleItemCommand>
{
  public RemoveScheduleRuleItemCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleRuleItemId)
        .NotEmpty().WithMessage("ScheduleRuleItemId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
