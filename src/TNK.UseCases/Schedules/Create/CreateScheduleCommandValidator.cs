using FluentValidation;
using System; // For DateOnly

namespace TNK.UseCases.Schedules.Create;

public class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
  public CreateScheduleCommandValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.Title)
        .NotEmpty().WithMessage("Schedule title is required.")
        .MaximumLength(255).WithMessage("Schedule title cannot exceed 255 characters.");

    RuleFor(x => x.EffectiveStartDate)
        .NotEmpty().WithMessage("Effective start date is required.");

    RuleFor(x => x.EffectiveEndDate)
        .GreaterThanOrEqualTo(x => x.EffectiveStartDate)
        .When(x => x.EffectiveEndDate.HasValue)
        .WithMessage("Effective end date must be on or after the effective start date.");

    RuleFor(x => x.TimeZoneId)
        .NotEmpty().WithMessage("TimeZoneId is required.")
        .MaximumLength(100).WithMessage("TimeZoneId cannot exceed 100 characters.")
        .Must(BeValidTimeZone).WithMessage("Invalid TimeZoneId provided.");
  }

  private bool BeValidTimeZone(string timeZoneId)
  {
    if (string.IsNullOrEmpty(timeZoneId))
    {
      return false; // Already handled by NotEmpty, but good for robustness
    }
    try
    {
      TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      return true;
    }
    catch (TimeZoneNotFoundException)
    {
      return false;
    }
    catch (InvalidTimeZoneException) // Handles cases like empty string on some platforms
    {
      return false;
    }
  }
}
