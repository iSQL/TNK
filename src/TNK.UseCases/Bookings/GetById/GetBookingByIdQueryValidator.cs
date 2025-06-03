using FluentValidation;
using System; // For Guid

namespace TNK.UseCases.Bookings.GetById;

public class GetBookingByIdQueryValidator : AbstractValidator<GetBookingByIdQuery>
{
  public GetBookingByIdQueryValidator()
  {
    RuleFor(x => x.BookingId)
        .NotEmpty().WithMessage("BookingId is required.");

    RuleFor(x => x.BusinessProfileId)
        .NotEmpty().WithMessage("BusinessProfileId is required for authorization.");
  }
}
