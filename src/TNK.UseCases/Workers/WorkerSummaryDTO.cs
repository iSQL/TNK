using System;

namespace TNK.UseCases.Workers;

/// <summary>
/// A summary DTO for representing a worker, typically for lists or nested data.
/// </summary>
public record WorkerSummaryDTO(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName, // Calculated from FirstName and LastName
    bool IsActive
);
