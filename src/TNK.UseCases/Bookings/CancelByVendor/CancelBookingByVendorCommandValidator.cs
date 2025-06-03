using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.CancelByVendor;

public class CancelBookingByVendorCommandValidator : AbstractValidator<CancelBookingByVendorCommand>
{
  public CancelBookingByVendorCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");

    RuleFor(x => x.CancellationReason)
        .NotEmpty().WithMessage("Cancellation reason is required.")
        .MinimumLength(5).WithMessage("Cancellation reason must be at least 5 characters.")
        .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");
  }
}
