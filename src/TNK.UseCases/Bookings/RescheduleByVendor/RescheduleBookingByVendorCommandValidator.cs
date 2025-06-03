using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.RescheduleByVendor;

public class RescheduleBookingByVendorCommandValidator : AbstractValidator<RescheduleBookingByVendorCommand>
{
  public RescheduleBookingByVendorCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.NewAvailabilitySlotId)
        .NotEmpty().WithMessage("NewAvailabilitySlotId is required.");

    RuleFor(x => x.NotesByVendor)
        .MaximumLength(255)
        .WithMessage($"Vendor notes cannot exceed 255 characters.")
        .When(x => !string.IsNullOrEmpty(x.NotesByVendor));
  }
}
