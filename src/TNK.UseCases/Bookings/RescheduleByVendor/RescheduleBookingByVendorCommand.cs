using Ardalis.Result;
using MediatR;
using TNK.UseCases.Bookings; // For BookingDTO
using System; // For Guid

namespace TNK.UseCases.Bookings.RescheduleByVendor;

public record RescheduleBookingByVendorCommand(
    Guid BookingId,          // The booking to be rescheduled
    int BusinessProfileId,   // For authorization
    Guid NewAvailabilitySlotId, // The ID of the NEW, available slot to move the booking to
    string? NotesByVendor = null // Optional notes from the vendor about the reschedule
) : IRequest<Result<BookingDTO>>; // Returns the updated BookingDTO
