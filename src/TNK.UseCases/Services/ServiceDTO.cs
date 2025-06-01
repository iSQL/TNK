namespace TNK.UseCases.Services;

public record ServiceDTO(
    Guid Id,
    int BusinessProfileId,
    string Name,
    string? Description,
    int DurationInMinutes,
    decimal Price,
    bool IsActive,
    string? ImageUrl
);
