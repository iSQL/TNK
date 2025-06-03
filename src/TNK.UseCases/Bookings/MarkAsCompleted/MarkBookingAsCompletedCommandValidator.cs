using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.MarkAsCompleted;

public class MarkBookingAsCompletedCommandValidator : AbstractValidator<MarkBookingAsCompletedCommand>
{
  public MarkBookingAsCompletedCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
