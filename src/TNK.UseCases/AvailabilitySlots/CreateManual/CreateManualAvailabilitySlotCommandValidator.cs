using FluentValidation;
using System; // For DateTime
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.UseCases.AvailabilitySlots.CreateManual;

public class CreateManualAvailabilitySlotCommandValidator : AbstractValidator<CreateManualAvailabilitySlotCommand>
{
  public CreateManualAvailabilitySlotCommandValidator()
  {
    RuleFor(x => x.WorkerId)
        .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.StartTime)
        .NotEmpty().WithMessage("Start time is required.")
        // Ensure StartTime is not in the past, allowing for a small buffer for clock differences if needed.
        // This rule might be too strict depending on how far in advance slots can be created.
        // Consider if slots can be created for today even if the current time has passed a bit.
        .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-5)).WithMessage("Start time cannot be in the past.");

    RuleFor(x => x.EndTime)
        .NotEmpty().WithMessage("End time is required.")
        .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.");

    RuleFor(x => x.Status)
        .IsInEnum().WithMessage("A valid status is required.")
        // Optionally restrict which statuses can be set on manual creation.
        // For example, 'Booked' status should likely not be set directly via this command.
        .Must(status => status == AvailabilitySlotStatus.Available || status == AvailabilitySlotStatus.Unavailable || status == AvailabilitySlotStatus.Break)
        .WithMessage("Manual creation status can only be Available, Unavailable, or Break.");
  }
}
