// src/TNK.UseCases/BusinessProfiles/UpdateAdmin/AdminUpdateBusinessProfileCommandHandler.cs
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging; // Optional: for logging
using TNK.Core.Interfaces;       // For IBusinessProfileRepository
using TNK.Core.BusinessAggregate; // For BusinessProfile entity
using TNK.UseCases.BusinessProfiles; // For BusinessProfileDTO
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.BusinessProfiles.UpdateAdmin;

/// <summary>
/// Handles the update of an existing Business Profile by its ID, initiated by a SuperAdmin.
/// </summary>
public class AdminUpdateBusinessProfileHandler : IRequestHandler<AdminUpdateBusinessProfileCommand, Result<BusinessProfileDTO>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly ILogger<AdminUpdateBusinessProfileHandler> _logger; // Optional

  public AdminUpdateBusinessProfileHandler(
      IBusinessProfileRepository businessProfileRepository,
      ILogger<AdminUpdateBusinessProfileHandler> logger) // Optional: ILogger
  {
    _businessProfileRepository = businessProfileRepository;
    _logger = logger; // Optional
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
      // Use the entity's own method to update its details, ensuring domain logic is applied
      businessProfileToUpdate.UpdateDetails(
          name: request.Name,
          address: request.Address,
          phoneNumber: request.PhoneNumber,
          description: request.Description
      ); //

      await _businessProfileRepository.UpdateAsync(businessProfileToUpdate, cancellationToken);
      // Note: SaveChangesAsync is typically called by the UpdateAsync method in EfRepository or by a Unit of Work pattern.

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
    catch (System.ArgumentException ex) // Catch validation errors from UpdateDetails if any
    {
      _logger.LogWarning(ex, "Validation error while updating Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.Invalid(new ValidationError(ex.ParamName ?? "RequestBody", ex.Message)); // Or a more generic error
    }
    catch (System.Exception ex)
    {
      _logger.LogError(ex, "Error occurred while updating Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.Error($"An error occurred while updating the Business Profile: {ex.Message}");
    }
  }
}
