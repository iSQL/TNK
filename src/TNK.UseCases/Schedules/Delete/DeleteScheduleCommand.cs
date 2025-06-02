using Ardalis.Result;
using MediatR;
using System; // For Guid

namespace TNK.UseCases.Schedules.Delete;

public record DeleteScheduleCommand(
    Guid ScheduleId,
    Guid WorkerId,          // For authorization context
    int BusinessProfileId  // For authorization context
) : IRequest<Result>; // Returns a simple Result (success/failure)
