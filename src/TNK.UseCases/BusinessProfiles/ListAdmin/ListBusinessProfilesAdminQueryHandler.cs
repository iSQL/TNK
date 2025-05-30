using MediatR;
using TNK.Core.Interfaces;

namespace TNK.UseCases.BusinessProfiles.ListAdmin;

public class ListBusinessProfilesAdminQueryHandler : IRequestHandler<ListBusinessProfilesAdminQuery, Result<Common.Models.PagedResult<BusinessProfileDTO>>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;

  public ListBusinessProfilesAdminQueryHandler(IBusinessProfileRepository businessProfileRepository)
  {
    _businessProfileRepository = businessProfileRepository;
  }

  public async Task<Result<Common.Models.PagedResult<BusinessProfileDTO>>> Handle(ListBusinessProfilesAdminQuery request, CancellationToken cancellationToken)
  {
    var filterSpec = new BusinessProfilesAdminFilterSpec(request.SearchTerm);
    int totalCount = await _businessProfileRepository.CountAsync(filterSpec, cancellationToken);

    var pagedSpec = new BusinessProfilesAdminSpec(request.PageNumber, request.PageSize, request.SearchTerm);
    var businessProfiles = await _businessProfileRepository.ListAsync(pagedSpec, cancellationToken);

    var dtos = businessProfiles.Select(bp => new BusinessProfileDTO
    (
        id: bp.Id,
        name: bp.Name,
        address: bp.Address,
        phoneNumber: bp.PhoneNumber, 
        description: bp.Description,
        vendorId: bp.VendorId
    )).ToList();

    var pagedResult = new Common.Models.PagedResult<BusinessProfileDTO>(dtos, totalCount, request.PageNumber, request.PageSize);

    return Result.Success(pagedResult);
  }
}
