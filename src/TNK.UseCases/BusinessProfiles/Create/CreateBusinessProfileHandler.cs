using MediatR;
using Microsoft.EntityFrameworkCore; 
using TNK.UseCases.BusinessProfile.Create; 


namespace TNK.UseCases.BusinessProfiles.Create;

/// <summary>
/// Handles the creation of a new business profile.
/// </summary>
public class CreateBusinessProfileHandler : IRequestHandler<CreateBusinessProfileCommand, Result<int>>
{
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

   
    var existingProfileSpec = new TNK.Core.BusinessAggregate.Specifications.BusinessProfileByVendorIdSpec(request.VendorId);
    var existingProfile = await _repository.FirstOrDefaultAsync(existingProfileSpec, cancellationToken);
    if (existingProfile != null)
    {
      return Result<int>.Conflict("A business profile for this vendor already exists.");
    }

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
      return Result<int>.Error($"A database error occurred while saving the business profile. This could be due to an existing profile for the vendor or other database constraints.");
    }
    catch (Exception) 
    {
      return Result<int>.Error($"An unexpected error occurred while creating the business profile. Please try again or contact support if the issue persists.");
    }
  }
}
