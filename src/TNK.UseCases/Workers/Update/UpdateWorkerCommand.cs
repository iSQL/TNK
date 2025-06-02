using Ardalis.Result;
using MediatR;
using TNK.UseCases.Workers; // For WorkerDTO

namespace TNK.UseCases.Workers.Update;

public record UpdateWorkerCommand(
    Guid WorkerId,          // ID of the worker to update
    int BusinessProfileId,  // BusinessProfileId for authorization
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    string? ImageUrl,
    string? Specialization,
    string? ApplicationUserId = null // Optional: Link/update link to an existing ApplicationUser
) : IRequest<Result<WorkerDTO>>; // Returns the updated WorkerDTO
