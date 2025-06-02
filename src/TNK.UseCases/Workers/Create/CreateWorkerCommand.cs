using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.Workers.Create;

// Command to create a new worker for a specific business profile.
public record CreateWorkerCommand(
    int BusinessProfileId, // The business this worker belongs to
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? ImageUrl,
    string? Specialization,
    string? ApplicationUserId = null // Optional: Link to an existing ApplicationUser
) : IRequest<Result<Guid>>; // Returns the ID of the newly created worker
