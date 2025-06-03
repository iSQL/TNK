using Ardalis.Result;
using MediatR;
using System;
using TNK.Core.ServiceManagementAggregate.Enums;
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO

namespace TNK.UseCases.AvailabilitySlots.Update;

public record UpdateAvailabilitySlotCommand(
    Guid AvailabilitySlotId,
    int BusinessProfileId, // For authorization
    Guid WorkerId,          // For authorization & context
    DateTime? NewStartTime, // Nullable if only status is changing
    DateTime? NewEndTime,   // Nullable if only status is changing
    AvailabilitySlotStatus? NewStatus // Nullable if only time is changing
) : IRequest<Result<AvailabilitySlotDTO>>;
