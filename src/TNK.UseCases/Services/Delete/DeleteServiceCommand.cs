using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.Services.Delete;

public record DeleteServiceCommand(
    Guid ServiceId,
    int BusinessProfileId // For authorization: to ensure the user owns the service being deleted
) : IRequest<Result>; // Returns a simple Result (success/failure)
