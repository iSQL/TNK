using MediatR;

namespace TNK.UseCases.BusinessProfiles.Update;

/// <summary>
/// Represents the command to update an existing business profile for a vendor.
/// </summary>
/// <param name="VendorId">The ID of the authenticated vendor whose profile is being updated.</param>
/// <param name="Name">The new name for the business profile. If null, the name is not changed.
/// <param name="Address">The new address for the business profile. Null if not changed.</param>
/// <param name="PhoneNumber">The new phone number. Null if not changed.</param>
/// <param name="Description">The new description. Null if not changed.</param>
public record UpdateBusinessProfileCommand(
    string VendorId,
    string? Name, 
    string? Address,
    string? PhoneNumber,
    string? Description) : IRequest<Result<BusinessProfileDTO>>; 
