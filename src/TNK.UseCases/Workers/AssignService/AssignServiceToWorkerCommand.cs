using Ardalis.Result;
using MediatR;
using System;

namespace TNK.UseCases.Workers.AssignService;

/// <summary>
/// Command to assign a service to a worker.
/// </summary>
public record AssignServiceToWorkerCommand(
    Guid WorkerId,
    Guid ServiceId,
    int BusinessProfileId // For authorization and context
) : IRequest<Result>; // Returns a simple success/failure result
