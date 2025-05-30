
using MediatR;
using TNK.Core.BusinessAggregate.Specifications; 

namespace TNK.UseCases.BusinessProfiles.GetMy;

public class GetMyBusinessProfileQueryHandler : IRequestHandler<GetMyBusinessProfileQuery, Result<BusinessProfileDTO?>>
{
  private readonly IReadRepository<TNK.Core.BusinessAggregate.BusinessProfile> _repository;

  public GetMyBusinessProfileQueryHandler(IReadRepository<TNK.Core.BusinessAggregate.BusinessProfile> repository)
  {
    _repository = repository;
  }

  public async Task<Result<BusinessProfileDTO?>> Handle(GetMyBusinessProfileQuery request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.VendorId))
    {
      return Result<BusinessProfileDTO?>.Invalid(new ValidationError { Identifier = nameof(request.VendorId), ErrorMessage = "Vendor ID is required." });
    }

    var spec = new BusinessProfileByVendorIdSpec(request.VendorId);
    var profileEntity = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (profileEntity == null)
    {
      return Result<BusinessProfileDTO?>.NotFound();
    }

    var profileDto = new BusinessProfileDTO(
        profileEntity.Id,
        profileEntity.Name,
        profileEntity.Address,
        profileEntity.PhoneNumber,
        profileEntity.Description,
        profileEntity.VendorId
    );

    return Result<BusinessProfileDTO?>.Success(profileDto);
  }
}
