using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using TNK.Core.BusinessAggregate; // Assuming BusinessProfileId is a Guid
using TNK.Core.Identity; // For ApplicationUser if Worker is linked to a system user

namespace TNK.Core.ServiceManagementAggregate.Entities;

public class Worker : EntityBase<Guid>, IAggregateRoot
{
  public int BusinessProfileId { get; private set; } // FK to BusinessProfile
  public virtual BusinessProfile? BusinessProfile { get; private set; } // Navigation property

  // Optional: If worker is also a platform user
  public string? ApplicationUserId { get; private set; }
  public virtual ApplicationUser? ApplicationUser { get; private set; }

  public string FirstName { get; private set; } = string.Empty;
  public string LastName { get; private set; } = string.Empty;
  public string FullName => $"{FirstName} {LastName}";
  public string? Email { get; private set; }
  public string? PhoneNumber { get; private set; }
  public bool IsActive { get; private set; }
  public string? ImageUrl { get; private set; } // Optional image for the worker
  public string? Specialization { get; private set; } // e.g., "Haircuts", "Massages"

  // Navigation properties
  public virtual ICollection<Schedule> Schedules { get; private set; } = new List<Schedule>();
  public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; private set; } = new List<AvailabilitySlot>();
  public virtual ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

  // Private constructor for EF Core
  private Worker() { }

  public Worker(int businessProfileId, string firstName, string lastName)
  {
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
    LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
    IsActive = true;
  }

  public void UpdateDetails(string firstName, string lastName, string? email, string? phoneNumber, string? imageUrl, string? specialization)
  {
    FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
    LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
    Email = email; // Basic validation for email format can be added
    PhoneNumber = phoneNumber;
    ImageUrl = imageUrl;
    Specialization = specialization;
  }

  public void LinkToApplicationUser(string applicationUserId)
  {
    ApplicationUserId = Guard.Against.NullOrWhiteSpace(applicationUserId, nameof(applicationUserId));
  }

  public void Activate() => IsActive = true;
  public void Deactivate() => IsActive = false;
}
