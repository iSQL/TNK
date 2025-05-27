using System.Linq; // Required for FirstOrDefault on result.Errors
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Ardalis.SharedKernel; // For IRepository
using MediatR;
using Microsoft.EntityFrameworkCore; // For DbUpdateException
using Microsoft.Extensions.Logging;
using TNK.Core.BusinessAggregate; // Correct using for the BusinessProfile ENTITY
using TNK.Core.BusinessAggregate.Specifications;
using TNK.UseCases.BusinessProfile.Create; // Correct using for BusinessProfileByVendorIdSpec

// Removed: using Npgsql; // UseCases should not depend on specific DB providers like Npgsql

namespace TNK.UseCases.BusinessProfiles.Create;

/// <summary>
/// Handles the creation of a new business profile.
/// </summary>
public class CreateBusinessProfileHandler : IRequestHandler<CreateBusinessProfileCommand, Result<int>>
{
  // Ensure this repository is for the correct BusinessProfile entity type
  private readonly IRepository<TNK.Core.BusinessAggregate.BusinessProfile> _repository;

  public CreateBusinessProfileHandler(IRepository<TNK.Core.BusinessAggregate.BusinessProfile> repository)
  {
    _repository = repository;
  }

  public async Task<Result<int>> Handle(CreateBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    // Basic validation (more complex validation can be on the command with FluentValidation)
    if (string.IsNullOrWhiteSpace(request.VendorId))
    {
      return Result<int>.Invalid(new ValidationError { ErrorMessage = "Vendor ID is required.", Identifier = nameof(request.VendorId) });
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
      return Result<int>.Invalid(new ValidationError { ErrorMessage = "Business name is required.", Identifier = nameof(request.Name) });
    }

    // Check if the vendor already has a business profile
    // Ensure BusinessProfileByVendorIdSpec is from TNK.Core.BusinessAggregate.Specifications
    var existingProfileSpec = new TNK.Core.BusinessAggregate.Specifications.BusinessProfileByVendorIdSpec(request.VendorId);
    // The _repository is IRepository<TNK.Core.BusinessAggregate.BusinessProfile>, so FirstOrDefaultAsync will expect a spec for that type.
    var existingProfile = await _repository.FirstOrDefaultAsync(existingProfileSpec, cancellationToken);
    if (existingProfile != null)
    {
      return Result<int>.Conflict("A business profile for this vendor already exists.");
    }

    // Ensure 'new BusinessProfile(...)' refers to TNK.Core.BusinessAggregate.BusinessProfile
    var newBusinessProfile = new TNK.Core.BusinessAggregate.BusinessProfile(
        vendorId: request.VendorId,
        name: request.Name,
        address: request.Address,
        phoneNumber: request.PhoneNumber,
        description: request.Description
    );

    try
    {
      var createdProfile = await _repository.AddAsync(newBusinessProfile, cancellationToken);

      if (createdProfile == null || createdProfile.Id <= 0)
      {
        return Result<int>.Error("Failed to save the business profile after creation or ID was not generated.");
      }
      return Result<int>.Success(createdProfile.Id);
    }
    catch (DbUpdateException) // Catch specific database update exceptions
    {
      // Log the exception (ex.ToString()) for full details
      // The Npgsql-specific check has been removed as it's an infrastructure concern.
      // The DbUpdateException itself can indicate issues like constraint violations.
      // A more generic message is appropriate here. If the unique constraint on VendorId
      // in the DB is hit, this exception will be thrown.
      return Result<int>.Error($"A database error occurred while saving the business profile. This could be due to an existing profile for the vendor or other database constraints.");
    }
    catch (Exception) // Catch any other unexpected errors
    {
      // Log the exception (ex.ToString()) for full details
      return Result<int>.Error($"An unexpected error occurred while creating the business profile. Please try again or contact support if the issue persists.");
    }
  }
}
