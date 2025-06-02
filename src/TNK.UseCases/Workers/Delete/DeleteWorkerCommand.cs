using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.Workers.Delete;

public record DeleteWorkerCommand(
    Guid WorkerId,
    int BusinessProfileId // For authorization: to ensure the user owns the worker being deleted
) : IRequest<Result>; // Returns a simple Result (success/failure)
