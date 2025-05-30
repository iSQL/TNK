using MediatR;
using Microsoft.Extensions.Logging; 
using TNK.Core.BusinessAggregate.Specifications; 
using TNK.Core.Identity; 
using TNK.Core.Interfaces; 

namespace TNK.UseCases.BusinessProfiles.CreateAdmin;

public class AdminCreateBusinessProfileCommandHandler : IRequestHandler<AdminCreateBusinessProfileCommand, Result<BusinessProfileDTO>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly IReadRepository<ApplicationUser> _userRepository; // To verify VendorId
  private readonly ILogger<AdminCreateBusinessProfileCommandHandler> _logger;

  public AdminCreateBusinessProfileCommandHandler(
      IBusinessProfileRepository businessProfileRepository,
      IReadRepository<ApplicationUser> userRepository,
      ILogger<AdminCreateBusinessProfileCommandHandler> logger)
  {
    _businessProfileRepository = businessProfileRepository;
    _userRepository = userRepository;
    _logger = logger; 
  }

  public async Task<Result<BusinessProfileDTO>> Handle(AdminCreateBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Attempting to create Business Profile for Vendor ID {VendorId} by SuperAdmin.", request.VendorId);

    var vendorUser = await _userRepository.GetByIdAsync(request.VendorId, cancellationToken);
    if (vendorUser == null)
    {
      _logger.LogWarning("Vendor user with ID {VendorId} not found.", request.VendorId);
      return Result.Invalid(new ValidationError("VendorId", $"Vendor user with ID {request.VendorId} not found."));
    }
    // Optional: Further check if vendorUser has the 'Vendor' role. This might require UserManager or different service.

    // Check if a BusinessProfile already exists for this VendorId
    var existingProfileSpec = new BusinessProfileByVendorIdSpec(request.VendorId); //
    var existingProfile = await _businessProfileRepository.FirstOrDefaultAsync(existingProfileSpec, cancellationToken);
    if (existingProfile != null)
    {
      _logger.LogWarning("Business Profile already exists for Vendor ID {VendorId}.", request.VendorId);
      return Result.Conflict($"A Business Profile already exists for Vendor ID {request.VendorId}.");
    }

    try
    {
      // Create new BusinessProfile entity
      var newBusinessProfile = new Core.BusinessAggregate.BusinessProfile(
          request.VendorId,
          request.Name,
          request.Address,
          request.PhoneNumber,
          request.Description
      );

      // Add to repository
      var createdProfile = await _businessProfileRepository.AddAsync(newBusinessProfile, cancellationToken);

      // Map to DTO
      var dto = new BusinessProfileDTO
      (
          id: createdProfile.Id,
          name: createdProfile.Name,
          address: createdProfile.Address,
          phoneNumber: createdProfile.PhoneNumber,
          description: createdProfile.Description,
          vendorId: createdProfile.VendorId
      ); 

      _logger.LogInformation("Business Profile created successfully with ID {BusinessProfileId} for Vendor ID {VendorId} by SuperAdmin.", createdProfile.Id, request.VendorId);
      return Result.Success(dto);
    }
    catch (System.ArgumentException ex) 
    {
      _logger.LogWarning(ex, "Validation error while creating Business Profile for Vendor ID {VendorId}.", request.VendorId);
      return Result.Invalid(new ValidationError(ex.ParamName ?? "RequestBody", ex.Message));
    }
    catch (System.Exception ex)
    {
      _logger.LogError(ex, "Error occurred while creating Business Profile for Vendor ID {VendorId}.", request.VendorId);
      return Result.Error($"An error occurred while creating the Business Profile: {ex.Message}");
    }
  }
}
