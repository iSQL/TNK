using Ardalis.Result;
using MediatR;
using TNK.UseCases.Bookings; // For BookingDTO
using System; // For Guid

namespace TNK.UseCases.Bookings.UpdateNotesByVendor;

public record UpdateBookingNotesByVendorCommand(
    Guid BookingId,
    int BusinessProfileId,   // For authorization
    string? VendorNotes      // The notes from the vendor; nullable to allow clearing notes
) : IRequest<Result<BookingDTO>>; // Returns the updated BookingDTO
