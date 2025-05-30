using MediatR;

namespace TNK.UseCases.BusinessProfiles.CreateAdmin;

/// <summary>
/// Represents a command to create a new Business Profile by a SuperAdmin for a specified Vendor.
/// </summary>
public record AdminCreateBusinessProfileCommand : IRequest<Result<BusinessProfileDTO>>
{
  public string VendorId { get; init; } = string.Empty; // ID of the ApplicationUser (vendor)
  public string Name { get; init; } = string.Empty;
  public string? Address { get; init; }
  public string? PhoneNumber { get; init; }
  public string? Description { get; init; }

  public AdminCreateBusinessProfileCommand(string vendorId, string name, string? address, string? phoneNumber, string? description)
  {
    VendorId = vendorId;
    Name = name;
    Address = address;
    PhoneNumber = phoneNumber;
    Description = description;
  }
}
