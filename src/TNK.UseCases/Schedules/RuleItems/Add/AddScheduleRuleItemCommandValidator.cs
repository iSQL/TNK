using FluentValidation;
using System; // For DayOfWeek, TimeOnly

namespace TNK.UseCases.Schedules.RuleItems.Add;

public class AddScheduleRuleItemCommandValidator : AbstractValidator<AddScheduleRuleItemCommand>
{
  public AddScheduleRuleItemCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.DayOfWeek)
        .IsInEnum().WithMessage("A valid DayOfWeek is required.");

    RuleFor(x => x.StartTime)
        .NotEmpty().WithMessage("Start time is required.");

    RuleFor(x => x.EndTime)
        .NotEmpty().WithMessage("End time is required.")
        .GreaterThan(x => x.StartTime)
        .When(x => x.IsWorkingDay) // Only if it's a working day
        .WithMessage("End time must be after start time for a working day.");

    // If not a working day, start/end times might not be as strictly validated against each other,
    // or could be set to a default like TimeOnly.MinValue if not applicable.
    // The entity's AddRuleItem method should handle this logic.
    // RuleFor(x => x.EndTime)
    //     .Equal(x => x.StartTime) // Or some other convention for non-working days
    //     .When(x => !x.IsWorkingDay)
    //     .WithMessage("For non-working days, start and end times should ideally be equal or defaults.");
  }
}
