// File: TNK.UseCases/Workers/WorkerDTO.cs
namespace TNK.UseCases.Workers;

public record WorkerDTO(
    Guid Id,
    int BusinessProfileId,
    string? ApplicationUserId, // Nullable, as a worker might not be a platform user
    string FirstName,
    string LastName,
    string FullName, // This can be populated during mapping
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    string? ImageUrl,
    string? Specialization
);
