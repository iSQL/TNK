// src/TNK.UseCases/BusinessProfiles/UpdateAdmin/AdminUpdateBusinessProfileCommand.cs
using Ardalis.Result;
using MediatR;

namespace TNK.UseCases.BusinessProfiles.UpdateAdmin;

/// <summary>
/// Represents a command to update an existing Business Profile by its ID, intended for SuperAdmin use.
/// </summary>
public record AdminUpdateBusinessProfileCommand : IRequest<Result<BusinessProfileDTO>>
{
  public int BusinessProfileId { get; init; }
  public string Name { get; init; } = string.Empty;
  public string? Address { get; init; }
  public string? PhoneNumber { get; init; }
  public string? Description { get; init; }

  // Constructor to ensure BusinessProfileId is provided
  public AdminUpdateBusinessProfileCommand(int businessProfileId, string name, string? address, string? phoneNumber, string? description)
  {
    BusinessProfileId = businessProfileId;
    Name = name;
    Address = address;
    PhoneNumber = phoneNumber;
    Description = description;
  }
}
