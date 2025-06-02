using Ardalis.Result;
using MediatR;
using System; // For Guid

namespace TNK.UseCases.Schedules.Overrides.Remove;

public record RemoveScheduleOverrideCommand(
    Guid ScheduleId,
    Guid ScheduleOverrideId, // The ID of the specific override to remove
    Guid WorkerId,           // For authorization context
    int BusinessProfileId   // For authorization context
) : IRequest<Result>; // Returns a simple Result (success/failure)
