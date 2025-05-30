using MediatR;
using TNK.Core.Interfaces; 

namespace TNK.UseCases.BusinessProfiles.GetByIdAdmin;

/// <summary>
/// Handles the retrieval of a specific Business Profile by its ID for a SuperAdmin.
/// </summary>
public class GetBusinessProfileByIdAdminQueryHandler : IRequestHandler<GetBusinessProfileByIdAdminQuery, Result<BusinessProfileDTO>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;

  public GetBusinessProfileByIdAdminQueryHandler(IBusinessProfileRepository businessProfileRepository)
  {
    _businessProfileRepository = businessProfileRepository;
  }

  public async Task<Result<BusinessProfileDTO>> Handle(GetBusinessProfileByIdAdminQuery request, CancellationToken cancellationToken)
  {
    var businessProfile = await _businessProfileRepository.GetByIdAsync(request.BusinessProfileId, cancellationToken);

    if (businessProfile == null)
    {
      return Result.NotFound($"No Business Profile found with ID {request.BusinessProfileId}");
    }

    var dto = new BusinessProfileDTO
    (
        id: businessProfile.Id,
        name: businessProfile.Name,
        address: businessProfile.Address,
        phoneNumber: businessProfile.PhoneNumber, 
        description: businessProfile.Description,
        vendorId: businessProfile.VendorId
    );

    return Result.Success(dto);
  }
}
