using MediatR;
using Microsoft.Extensions.Logging; 
using TNK.Core.BusinessAggregate.Specifications; 

namespace TNK.UseCases.BusinessProfiles.Update;

/// <summary>
/// Handles the command to update an existing business profile.
/// </summary>
public class UpdateBusinessProfileHandler : IRequestHandler<UpdateBusinessProfileCommand, Result<BusinessProfileDTO>>
{
  private readonly IRepository<TNK.Core.BusinessAggregate.BusinessProfile> _repository;
  private readonly ILogger<UpdateBusinessProfileHandler> _logger;

  public UpdateBusinessProfileHandler(IRepository<TNK.Core.BusinessAggregate.BusinessProfile> repository, ILogger<UpdateBusinessProfileHandler> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public async Task<Result<BusinessProfileDTO>> Handle(UpdateBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.VendorId))
    {
      _logger.LogWarning("UpdateBusinessProfileCommand failed: VendorId is null or whitespace.");
      return Result<BusinessProfileDTO>.Invalid(new ValidationError { Identifier = nameof(request.VendorId), ErrorMessage = "Vendor ID is required." });
    }

    _logger.LogInformation("Attempting to update business profile for VendorId: {VendorId}", request.VendorId);

    var spec = new BusinessProfileByVendorIdSpec(request.VendorId);
    var existingProfile = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (existingProfile == null)
    {
      _logger.LogWarning("No business profile found for VendorId: {VendorId} during update.", request.VendorId);
      return Result<BusinessProfileDTO>.NotFound("Business profile not found for this vendor.");
    }

    string newName = existingProfile.Name; 
    if (!string.IsNullOrWhiteSpace(request.Name))
    {
      newName = request.Name;
    }
    else if (request.Name == null) // If explicitly passed as null, but not whitespace, it implies "don't change"
    {
      
    }
  
    try
    {
      existingProfile.UpdateDetails(
          name: newName,
          address: request.Address, 
          phoneNumber: request.PhoneNumber,
          description: request.Description
      );
    }
    catch (ArgumentException ex) 
    {
      _logger.LogWarning(ex, "Validation error during business profile update for VendorId: {VendorId}", request.VendorId);
      return Result<BusinessProfileDTO>.Invalid(new ValidationError { Identifier = ex.ParamName ?? "ProfileData", ErrorMessage = ex.Message });
    }


    await _repository.UpdateAsync(existingProfile, cancellationToken);
    _logger.LogInformation("Business profile updated successfully for VendorId: {VendorId}, ProfileId: {ProfileId}", request.VendorId, existingProfile.Id);

    var updatedDto = new BusinessProfileDTO
    {
      Id = existingProfile.Id,
      Name = existingProfile.Name,
      Address = existingProfile.Address,
      PhoneNumber = existingProfile.PhoneNumber,
      Description = existingProfile.Description,
      VendorId = existingProfile.VendorId
    };

    return Result<BusinessProfileDTO>.Success(updatedDto);
  }
}
