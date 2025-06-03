using FluentValidation;
using System;
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.UseCases.AvailabilitySlots.Update;

public class UpdateAvailabilitySlotCommandValidator : AbstractValidator<UpdateAvailabilitySlotCommand>
{
  public UpdateAvailabilitySlotCommandValidator()
  {
    RuleFor(x => x.AvailabilitySlotId)
        .NotEmpty().WithMessage("AvailabilitySlotId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required for context.");

    // Rule to ensure at least one updatable field is provided
    RuleFor(x => x)
        .Must(x => x.NewStartTime.HasValue || x.NewEndTime.HasValue || x.NewStatus.HasValue)
        .WithMessage("At least one field (NewStartTime, NewEndTime, or NewStatus) must be provided for update.");

    When(x => x.NewStartTime.HasValue || x.NewEndTime.HasValue, () => {
      RuleFor(x => x.NewStartTime)
          .NotNull().WithMessage("NewStartTime is required if NewEndTime is provided.")
          .When(x => x.NewEndTime.HasValue);

      RuleFor(x => x.NewEndTime)
          .NotNull().WithMessage("NewEndTime is required if NewStartTime is provided.")
          .When(x => x.NewStartTime.HasValue);

      RuleFor(x => x.NewEndTime)
          .GreaterThan(x => x.NewStartTime)
          .When(x => x.NewStartTime.HasValue && x.NewEndTime.HasValue)
          .WithMessage("NewEndTime must be after NewStartTime.");

      RuleFor(x => x.NewStartTime)
          // Consider if past updates are allowed for corrections. For now, allowing.
          .LessThan(DateTime.UtcNow.AddYears(5)) // Sanity check for future dates
          .When(x => x.NewStartTime.HasValue);
    });

    RuleFor(x => x.NewStatus)
        .IsInEnum().When(x => x.NewStatus.HasValue)
        .WithMessage("A valid status is required if provided.");

    // Prevent directly setting status to Booked via this generic update command
    // Booking should happen via a booking process which calls slot.BookSlot(bookingId)
    RuleFor(x => x.NewStatus)
        .NotEqual(AvailabilitySlotStatus.Booked)
        .When(x => x.NewStatus.HasValue)
        .WithMessage($"Status cannot be directly changed to 'Booked' via this update. Use the booking process.");
  }
}
