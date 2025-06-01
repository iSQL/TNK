using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.Services.Create;

// Represents the command to create a new service.
// The BusinessProfileId should be determined, possibly from the authenticated user's claims
// or passed explicitly if an admin is creating it for a specific business.
// For a vendor owner, this would typically be their own BusinessProfileId.
public record CreateServiceCommand(
    int BusinessProfileId, // Changed to int
    string Name,
    string? Description,
    int DurationInMinutes,
    decimal Price,
    string? ImageUrl
// bool IsActive is typically true by default on creation, can be set in handler
) : IRequest<Result<Guid>>; // Returns the ID of the newly created service
