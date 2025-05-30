using MediatR;
using Microsoft.Extensions.Logging; 
using TNK.Core.Interfaces;       

namespace TNK.UseCases.BusinessProfiles.UpdateAdmin;

/// <summary>
/// Handles the update of an existing Business Profile by its ID, initiated by a SuperAdmin.
/// </summary>
public class AdminUpdateBusinessProfileHandler : IRequestHandler<AdminUpdateBusinessProfileCommand, Result<BusinessProfileDTO>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly ILogger<AdminUpdateBusinessProfileHandler> _logger; 

  public AdminUpdateBusinessProfileHandler(
      IBusinessProfileRepository businessProfileRepository,
      ILogger<AdminUpdateBusinessProfileHandler> logger) 
  {
    _businessProfileRepository = businessProfileRepository;
    _logger = logger; 
  }

  public async Task<Result<BusinessProfileDTO>> Handle(AdminUpdateBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Attempting to update Business Profile with ID {BusinessProfileId} by SuperAdmin.", request.BusinessProfileId);

    var businessProfileToUpdate = await _businessProfileRepository.GetByIdAsync(request.BusinessProfileId, cancellationToken);

    if (businessProfileToUpdate == null)
    {
      _logger.LogWarning("Business Profile with ID {BusinessProfileId} not found for update.", request.BusinessProfileId);
      return Result.NotFound($"No Business Profile found with ID {request.BusinessProfileId} to update.");
    }

    try
    {
      businessProfileToUpdate.UpdateDetails(
          name: request.Name,
          address: request.Address,
          phoneNumber: request.PhoneNumber,
          description: request.Description
      ); //

      await _businessProfileRepository.UpdateAsync(businessProfileToUpdate, cancellationToken);

      var updatedDto = new BusinessProfileDTO
      (
          id: businessProfileToUpdate.Id,
          name: businessProfileToUpdate.Name,
          address: businessProfileToUpdate.Address,
          phoneNumber: businessProfileToUpdate.PhoneNumber,
          description: businessProfileToUpdate.Description,
          vendorId: businessProfileToUpdate.VendorId
      ); //

      _logger.LogInformation("Business Profile with ID {BusinessProfileId} was updated successfully by a SuperAdmin.", request.BusinessProfileId);
      return Result.Success(updatedDto);
    }
    catch (System.ArgumentException ex) 
    {
      _logger.LogWarning(ex, "Validation error while updating Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.Invalid(new ValidationError(ex.ParamName ?? "RequestBody", ex.Message));
    }
    catch (System.Exception ex)
    {
      _logger.LogError(ex, "Error occurred while updating Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.Error($"An error occurred while updating the Business Profile: {ex.Message}");
    }
  }
}
