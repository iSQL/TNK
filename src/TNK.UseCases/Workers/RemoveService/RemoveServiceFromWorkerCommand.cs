using Ardalis.Result;
using MediatR;
using System;

namespace TNK.UseCases.Workers.RemoveService;

/// <summary>
/// Command to remove/unassign a service from a worker.
/// </summary>
public record RemoveServiceFromWorkerCommand(
    Guid WorkerId,
    Guid ServiceId,
    int BusinessProfileId // For authorization and context
) : IRequest<Result>;
