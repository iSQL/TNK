namespace TNK.UseCases.BusinessProfiles;

/// <summary>
/// Represents a Data Transfer Object for a Business Profile.
/// This is typically used for returning business profile data from queries.
/// </summary>
public class BusinessProfileDTO
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Address { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Description { get; set; }
  public string VendorId { get; set; } = string.Empty; // The ID of the user who owns this profile

  // You might also include related data if necessary, for example:
  // public string VendorName { get; set; } = string.Empty; // If you join with user data

  public BusinessProfileDTO(int id, string name, string? address, string? phoneNumber, string? description, string vendorId)
  {
    Id = id;
    Name = name;
    Address = address;
    PhoneNumber = phoneNumber;
    Description = description;
    VendorId = vendorId;
  }

  // Parameterless constructor for some serialization scenarios or manual mapping
  public BusinessProfileDTO() { }
}
