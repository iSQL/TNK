using FluentValidation;
using System; // For DateOnly, TimeZoneInfo, TimeZoneNotFoundException, InvalidTimeZoneException

namespace TNK.UseCases.Schedules.UpdateInfo;

public class UpdateScheduleInfoCommandValidator : AbstractValidator<UpdateScheduleInfoCommand>
{
  public UpdateScheduleInfoCommandValidator()
  {
    RuleFor(x => x.ScheduleId)
        .NotEmpty().WithMessage("ScheduleId is required.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

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
    if (string.IsNullOrEmpty(timeZoneId)) return false;
    try
    {
      TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      return true;
    }
    catch (TimeZoneNotFoundException)
    {
      return false;
    }
    catch (InvalidTimeZoneException)
    {
      return false;
    }
  }
}
