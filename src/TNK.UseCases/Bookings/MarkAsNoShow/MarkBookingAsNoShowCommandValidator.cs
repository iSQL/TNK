using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.MarkAsNoShow;

public class MarkBookingAsNoShowCommandValidator : AbstractValidator<MarkBookingAsNoShowCommand>
{
  public MarkBookingAsNoShowCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
