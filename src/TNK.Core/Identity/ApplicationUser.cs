using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using TNK.Core.BusinessAggregate; // Optional: for basic annotations

namespace TNK.Core.Identity; // Or TNK.Core.UserAggregate;

public class ApplicationUser : IdentityUser
{
  [MaxLength(100)]
  public string? FirstName { get; set; }

  [MaxLength(100)]
  public string? LastName { get; set; }

  // You can add other custom properties here if needed for all users.
  // For example:
  // public DateTime DateOfBirth { get; set; }
  // public string? ProfilePictureUrl { get; set; }

  // Navigation property for the business profile if a user is a vendor
  // This assumes a one-to-one relationship from a vendor user to a BusinessProfile.
  // If a user can have multiple business profiles (unlikely for Phase 1), this would be ICollection<BusinessProfile>.
  public virtual BusinessProfile? BusinessProfile { get; set; }
}
