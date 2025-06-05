using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using System.Collections.Generic; // Required for ICollection
using TNK.Core.BusinessAggregate;
using TNK.Core.Identity;

namespace TNK.Core.ServiceManagementAggregate.Entities;

public class Worker : EntityBase<Guid>, IAggregateRoot
{
  public int BusinessProfileId { get; private set; }
  public virtual BusinessProfile? BusinessProfile { get; private set; }

  public string? ApplicationUserId { get; private set; }
  public virtual ApplicationUser? ApplicationUser { get; private set; }

  public string FirstName { get; private set; } = string.Empty;
  public string LastName { get; private set; } = string.Empty;
  public string FullName => $"{FirstName} {LastName}";
  public string? Email { get; private set; }
  public string? PhoneNumber { get; private set; }
  public bool IsActive { get; private set; }
  public string? ImageUrl { get; private set; }
  public string? Specialization { get; private set; }

  // Navigation properties
  public virtual ICollection<Schedule> Schedules { get; private set; } = new List<Schedule>();
  public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; private set; } = new List<AvailabilitySlot>();
  public virtual ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

  // --- NEW: Many-to-many relationship with Service ---
  public virtual ICollection<Service> Services { get; private set; } = new List<Service>();

  private Worker() { }

  public Worker(int businessProfileId, string firstName, string lastName)
  {
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
    LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
    IsActive = true;
    // Services collection is initialized above
  }

  public void UpdateDetails(string firstName, string lastName, string? email, string? phoneNumber, string? imageUrl, string? specialization)
  {
    FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
    LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
    Email = email;
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

  // --- NEW: Methods to manage services for this worker ---
  public void AddService(Service service)
  {
    Guard.Against.Null(service, nameof(service));
    if (!Services.Any(s => s.Id == service.Id))
    {
      Services.Add(service);
    }
  }

  public void RemoveService(Service service)
  {
    Guard.Against.Null(service, nameof(service));
    var existingService = Services.FirstOrDefault(s => s.Id == service.Id);
    if (existingService != null)
    {
      Services.Remove(existingService);
    }
  }

  public void ClearServices()
  {
    Services.Clear();
  }
}
