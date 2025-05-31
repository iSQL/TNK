using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.BusinessProfile.Create;

public record CreateBusinessProfileCommand(
    string VendorId,
    string Name,
    string? Address,
    string? PhoneNumber,
    string? Description) : IRequest<Result<int>>;
