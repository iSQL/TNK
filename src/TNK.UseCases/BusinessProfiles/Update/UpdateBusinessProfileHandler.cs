using Ardalis.Result;
using Ardalis.SharedKernel; // For IRepository
using MediatR;
using Microsoft.Extensions.Logging; // For ILogger
using System.Threading;
using System.Threading.Tasks;
using TNK.Core.BusinessAggregate; // Your BusinessProfile ENTITY
using TNK.Core.BusinessAggregate.Specifications; // For BusinessProfileByVendorIdSpec
using TNK.UseCases.BusinessProfiles; // For BusinessProfileDTO

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

    // Apply updates from the command to the entity.
    // If a command property is null, it means the client did not intend to update that field,
    // so we keep the existing entity value.
    // For 'Name', which is required on the entity, if request.Name is provided (not null and not whitespace), update it.
    // Otherwise, keep the existing name.

    string newName = existingProfile.Name; // Default to existing name
    if (!string.IsNullOrWhiteSpace(request.Name))
    {
      newName = request.Name;
    }
    else if (request.Name == null) // If explicitly passed as null, but not whitespace, it implies "don't change"
    {
      // Keep existingProfile.Name - already set in newName
    }
    // If request.Name is empty string "", this logic will keep the old name.
    // Adjust if empty string should clear the name (if entity allows it).
    // Assuming BusinessProfile entity has an UpdateDetails method or similar.
    // If not, update properties directly:
    // existingProfile.Name = newName; // This would be direct property update
    // existingProfile.Address = request.Address ?? existingProfile.Address; // Example for optional fields
    // existingProfile.PhoneNumber = request.PhoneNumber ?? existingProfile.PhoneNumber;
    // existingProfile.Description = request.Description ?? existingProfile.Description;

    // Using the UpdateDetails method on the entity is preferred if it exists and handles validation/logic.
    // For this example, let's assume an UpdateDetails method on the BusinessProfile entity.
    // If it doesn't exist, you'll need to create it or update properties directly.
    try
    {
      existingProfile.UpdateDetails(
          name: newName, // Use the determined newName
          address: request.Address, // Pass null if not provided, entity method should handle
          phoneNumber: request.PhoneNumber,
          description: request.Description
      );
    }
    catch (ArgumentException ex) // Catch validation errors from entity's update method
    {
      _logger.LogWarning(ex, "Validation error during business profile update for VendorId: {VendorId}", request.VendorId);
      return Result<BusinessProfileDTO>.Invalid(new ValidationError { Identifier = ex.ParamName ?? "ProfileData", ErrorMessage = ex.Message });
    }


    await _repository.UpdateAsync(existingProfile, cancellationToken);
    // In many Ardalis template setups, SaveChangesAsync is called by the DbContext override
    // or a Unit of Work pattern after MediatR dispatch.
    // If your IRepository.UpdateAsync doesn't trigger SaveChanges, you might need it here or ensure it's called.
    // For example: await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

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
