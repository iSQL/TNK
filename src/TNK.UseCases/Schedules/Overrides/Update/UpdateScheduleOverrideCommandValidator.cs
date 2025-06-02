using FluentValidation;
using System; // For DateOnly, TimeOnly

namespace TNK.UseCases.Schedules.Overrides.Update;

public class UpdateScheduleOverrideCommandValidator : AbstractValidator<UpdateScheduleOverrideCommand>
{
  public UpdateScheduleOverrideCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.ScheduleOverrideId)
        .NotEmpty().WithMessage("ScheduleOverrideId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.OverrideDate)
        .NotEmpty().WithMessage("OverrideDate is required for context and validation.");

    RuleFor(x => x.Reason)
        .NotEmpty().WithMessage("Reason for the override is required.")
        .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");

    When(x => x.IsWorkingDay, () =>
    {
      RuleFor(x => x.StartTime)
          .NotNull().WithMessage("Start time is required for a working day override.")
          .NotEmpty().WithMessage("Start time cannot be empty for a working day override.");

      RuleFor(x => x.EndTime)
          .NotNull().WithMessage("End time is required for a working day override.")
          .NotEmpty().WithMessage("End time cannot be empty for a working day override.")
          .GreaterThan(x => x.StartTime)
          .When(x => x.StartTime.HasValue) // Ensure StartTime has a value before comparing
          .WithMessage("End time must be after start time for a working day override.");
    });

    When(x => !x.IsWorkingDay, () =>
    {
      RuleFor(x => x.StartTime)
          .Null().WithMessage("Start time must be null for a non-working day override.");
      RuleFor(x => x.EndTime)
          .Null().WithMessage("End time must be null for a non-working day override.");
    });
  }
}
