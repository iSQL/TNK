using Ardalis.Result;
using MediatR;
using TNK.UseCases.Bookings; // For BookingDTO
using System; // For Guid

namespace TNK.UseCases.Bookings.MarkAsCompleted;

public record MarkBookingAsCompletedCommand(
    Guid BookingId,
    int BusinessProfileId // For authorization
) : IRequest<Result<BookingDTO>>; // Returns the updated BookingDTO
