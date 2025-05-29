using Ardalis.Result;
using Ardalis.SharedKernel; // For IReadRepository
using MediatR;
using Microsoft.Extensions.Logging; // Optional
using TNK.Core.BusinessAggregate; // For BusinessProfile entity
using TNK.Core.BusinessAggregate.Specifications; // For BusinessProfileByVendorIdSpec
using TNK.Core.Identity; // For ApplicationUser
using TNK.Core.Interfaces; // For IBusinessProfileRepository
using TNK.UseCases.BusinessProfiles; // For BusinessProfileDTO
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.BusinessProfiles.CreateAdmin;

public class AdminCreateBusinessProfileCommandHandler : IRequestHandler<AdminCreateBusinessProfileCommand, Result<BusinessProfileDTO>>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly IReadRepository<ApplicationUser> _userRepository; // To verify VendorId
  private readonly ILogger<AdminCreateBusinessProfileCommandHandler> _logger; // Optional

  public AdminCreateBusinessProfileCommandHandler(
      IBusinessProfileRepository businessProfileRepository,
      IReadRepository<ApplicationUser> userRepository,
      ILogger<AdminCreateBusinessProfileCommandHandler> logger) // Optional
  {
    _businessProfileRepository = businessProfileRepository;
    _userRepository = userRepository;
    _logger = logger; // Optional
  }

  public async Task<Result<BusinessProfileDTO>> Handle(AdminCreateBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Attempting to create Business Profile for Vendor ID {VendorId} by SuperAdmin.", request.VendorId);

    // 1. Verify VendorId exists
    var vendorUser = await _userRepository.GetByIdAsync(request.VendorId, cancellationToken);
    if (vendorUser == null)
    {
      _logger.LogWarning("Vendor user with ID {VendorId} not found.", request.VendorId);
      return Result.Invalid(new ValidationError("VendorId", $"Vendor user with ID {request.VendorId} not found."));
    }
    // Optional: Further check if vendorUser has the 'Vendor' role. This might require UserManager or different service.

    // 2. Check if a BusinessProfile already exists for this VendorId
    var existingProfileSpec = new BusinessProfileByVendorIdSpec(request.VendorId); //
    var existingProfile = await _businessProfileRepository.FirstOrDefaultAsync(existingProfileSpec, cancellationToken);
    if (existingProfile != null)
    {
      _logger.LogWarning("Business Profile already exists for Vendor ID {VendorId}.", request.VendorId);
      return Result.Conflict($"A Business Profile already exists for Vendor ID {request.VendorId}.");
    }

    try
    {
      // 3. Create new BusinessProfile entity
      var newBusinessProfile = new Core.BusinessAggregate.BusinessProfile(
          request.VendorId,
          request.Name,
          request.Address,
          request.PhoneNumber,
          request.Description
      );

      // 4. Add to repository
      var createdProfile = await _businessProfileRepository.AddAsync(newBusinessProfile, cancellationToken);
      // Note: SaveChangesAsync is typically called by AddAsync in EfRepository or by a Unit of Work.

      // 5. Map to DTO
      var dto = new BusinessProfileDTO
      (
          id: createdProfile.Id,
          name: createdProfile.Name,
          address: createdProfile.Address,
          phoneNumber: createdProfile.PhoneNumber,
          description: createdProfile.Description,
          vendorId: createdProfile.VendorId
      ); //

      _logger.LogInformation("Business Profile created successfully with ID {BusinessProfileId} for Vendor ID {VendorId} by SuperAdmin.", createdProfile.Id, request.VendorId);
      return Result.Success(dto);
    }
    catch (System.ArgumentException ex) // Catch validation errors from BusinessProfile constructor if any
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
