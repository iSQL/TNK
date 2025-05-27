using Ardalis.Result;
using MediatR;
using TNK.UseCases.BusinessProfiles; // For BusinessProfileDTO

namespace TNK.UseCases.BusinessProfiles.GetMy;

public record GetMyBusinessProfileQuery(string VendorId) : IRequest<Result<BusinessProfileDTO?>>;
