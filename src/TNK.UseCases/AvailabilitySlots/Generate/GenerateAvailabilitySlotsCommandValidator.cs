using FluentValidation;
using System;

namespace TNK.UseCases.AvailabilitySlots.Generate;

public class GenerateAvailabilitySlotsCommandValidator : AbstractValidator<GenerateAvailabilitySlotsCommand>
{
  public GenerateAvailabilitySlotsCommandValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required.");

    RuleFor(x => x.StartDate)
        .NotEmpty().WithMessage("Start date is required.");

    RuleFor(x => x.EndDate)
        .NotEmpty().WithMessage("End date is required.")
        .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

    RuleFor(x => x.SlotDurationInMinutes)
        .GreaterThan(0).WithMessage("Slot duration must be greater than 0 minutes.");

    // Validate that EndDate is not excessively far in the future (e.g., max 1 year)
    RuleFor(x => x.EndDate)
        .LessThanOrEqualTo(x => x.StartDate.AddYears(1)) // Example: Max 1 year generation period
        .WithMessage("The date range for slot generation cannot exceed 1 year.");
  }
}
