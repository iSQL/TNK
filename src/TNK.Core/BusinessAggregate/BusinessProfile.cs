using Ardalis.SharedKernel; 
using System.ComponentModel.DataAnnotations;
using TNK.Core.Identity;

namespace TNK.Core.BusinessAggregate;

public class BusinessProfile : EntityBase, IAggregateRoot
{
  [Required]
  [MaxLength(200)]
  public string Name { get; private set; } = string.Empty;

  [MaxLength(500)]
  public string? Address { get; private set; }

  [MaxLength(30)]
  public string? PhoneNumber { get; private set; }

  [MaxLength(2000)]
  public string? Description { get; private set; }

  [Required]
  public string VendorId { get; private set; } = string.Empty;

  public virtual ApplicationUser Vendor { get; private set; } = null!;

  private BusinessProfile() { }

  public BusinessProfile(string vendorId, string name, string? address = null, string? phoneNumber = null, string? description = null)
  {
    VendorId = vendorId ?? throw new ArgumentNullException(nameof(vendorId));
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Address = address;
    PhoneNumber = phoneNumber;
    Description = description;
  }

  public void UpdateDetails(string name, string? address, string? phoneNumber, string? description)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Address = address;
    PhoneNumber = phoneNumber;
    Description = description;
  }
}
