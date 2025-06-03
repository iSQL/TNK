using System;
using TNK.Core.ServiceManagementAggregate.Enums; // For AvailabilitySlotStatus

namespace TNK.UseCases.AvailabilitySlots;

public record AvailabilitySlotDTO(
    Guid Id,
    Guid WorkerId,
    int BusinessProfileId, // Denormalized in entity
    DateTime StartTime,    // Specific start date and time
    DateTime EndTime,      // Specific end date and time
    AvailabilitySlotStatus Status,
    Guid? BookingId        // If booked, the ID of the booking
);
