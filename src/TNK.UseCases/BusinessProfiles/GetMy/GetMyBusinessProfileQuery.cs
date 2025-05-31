using MediatR;

namespace TNK.UseCases.BusinessProfiles.GetMy;

public record GetMyBusinessProfileQuery(string VendorId) : IRequest<Result<BusinessProfileDTO?>>;
