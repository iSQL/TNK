using Ardalis.Result;
using MediatR;
using TNK.UseCases.Services; // For ServiceDto

namespace TNK.UseCases.Services.Update;

public record UpdateServiceCommand(
    Guid ServiceId,         // Id of the service to update
    string Name,
    string? Description,
    int DurationInMinutes,
    decimal Price,
    bool IsActive,          // Allow updating active status
    string? ImageUrl,
    int BusinessProfileId   // Included for authorization/scoping, to ensure vendor owns the service
) : IRequest<Result<ServiceDTO>>; // Returns the updated ServiceDto
