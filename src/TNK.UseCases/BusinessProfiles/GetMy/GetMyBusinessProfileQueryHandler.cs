
using MediatR;
using TNK.Core.BusinessAggregate;
using TNK.Core.BusinessAggregate.Specifications; // Correct using for BusinessProfileByVendorIdSpec


namespace TNK.UseCases.BusinessProfiles.GetMy;

public class GetMyBusinessProfileQueryHandler : IRequestHandler<GetMyBusinessProfileQuery, Result<BusinessProfileDTO?>>
{
  // Ensure this repository is for the correct BusinessProfile entity type
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

    // Ensure BusinessProfileByVendorIdSpec is from TNK.Core.BusinessAggregate.Specifications
    var spec = new BusinessProfileByVendorIdSpec(request.VendorId);

    // _repository is IReadRepository<TNK.Core.BusinessAggregate.BusinessProfile>
    // so FirstOrDefaultAsync will correctly use the spec for that entity type.
    var profileEntity = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (profileEntity == null)
    {
      return Result<BusinessProfileDTO?>.NotFound();
    }

    // Map the TNK.Core.BusinessAggregate.BusinessProfile entity to BusinessProfileDTO
    var profileDto = new BusinessProfileDTO
    {
      Id = profileEntity.Id,
      Name = profileEntity.Name,
      Address = profileEntity.Address,
      PhoneNumber = profileEntity.PhoneNumber,
      Description = profileEntity.Description,
      VendorId = profileEntity.VendorId
    };
    // Or if your DTO has a matching constructor:
    // var profileDto = new BusinessProfileDTO(
    //     profileEntity.Id,
    //     profileEntity.Name,
    //     profileEntity.Address,
    //     profileEntity.PhoneNumber,
    //     profileEntity.Description,
    //     profileEntity.VendorId
    // );

    return Result<BusinessProfileDTO?>.Success(profileDto);
  }
}
