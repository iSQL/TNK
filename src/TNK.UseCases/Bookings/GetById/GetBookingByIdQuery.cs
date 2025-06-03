using Ardalis.Result;
using MediatR;
using TNK.UseCases.Bookings; // For BookingDTO
using System; // For Guid

namespace TNK.UseCases.Bookings.GetById;

public record GetBookingByIdQuery(
    Guid BookingId,
    int BusinessProfileId // For authorization to ensure the booking belongs to this business
) : IRequest<Result<BookingDTO>>;
