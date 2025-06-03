using Ardalis.Result;
using MediatR;
using TNK.UseCases.Bookings; // For BookingDTO
using System; // For Guid

namespace TNK.UseCases.Bookings.CancelByVendor;

public record CancelBookingByVendorCommand(
    Guid BookingId,
    int BusinessProfileId,   // For authorization
    string CancellationReason // Reason provided by the vendor
) : IRequest<Result<BookingDTO>>; // Returns the updated BookingDTO
