using Ardalis.Result;
using MediatR;
using System;
using TNK.Core.ServiceManagementAggregate.Enums; // For AvailabilitySlotStatus
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO

namespace TNK.UseCases.AvailabilitySlots.CreateManual;

public record CreateManualAvailabilitySlotCommand(
    Guid WorkerId,
    int BusinessProfileId, // For authorization
    DateTime StartTime,
    DateTime EndTime,
    // Status is typically 'Available' on manual creation by vendor,
    // or 'Unavailable' if they are blocking time.
    // Let's default to Available and allow it to be specified if needed for blocking.
    AvailabilitySlotStatus Status = AvailabilitySlotStatus.Available
) : IRequest<Result<AvailabilitySlotDTO>>; // Returns the DTO of the created slot
