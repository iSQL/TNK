using Ardalis.Result;
using MediatR;
using System; // For Guid

namespace TNK.UseCases.AvailabilitySlots.Delete;

public record DeleteAvailabilitySlotCommand(
    Guid AvailabilitySlotId,
    Guid WorkerId,          // For authorization context
    int BusinessProfileId  // For authorization context
) : IRequest<Result>; // Returns a simple Result (success/failure)
