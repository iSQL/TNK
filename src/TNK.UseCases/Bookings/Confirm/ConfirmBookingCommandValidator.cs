using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.Confirm;

public class ConfirmBookingCommandValidator : AbstractValidator<ConfirmBookingCommand>
{
  public ConfirmBookingCommandValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
