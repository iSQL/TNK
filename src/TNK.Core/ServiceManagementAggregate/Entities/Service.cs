using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using System.Collections.Generic; // Required for ICollection
using TNK.Core.BusinessAggregate;

namespace TNK.Core.ServiceManagementAggregate.Entities;

public class Service : EntityBase<Guid>, IAggregateRoot
{
  public int BusinessProfileId { get; private set; }
  public virtual BusinessProfile? BusinessProfile { get; private set; }

  public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public int DurationInMinutes { get; private set; }
  public decimal Price { get; private set; }
  public bool IsActive { get; private set; }
  public string? ImageUrl { get; private set; }

  // --- NEW: Many-to-many relationship with Worker ---
  public virtual ICollection<Worker> Workers { get; private set; } = new List<Worker>();

  private Service() { }

  public Service(int businessProfileId, string name, int durationInMinutes, decimal price)
  {
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    DurationInMinutes = Guard.Against.NegativeOrZero(durationInMinutes, nameof(durationInMinutes));
    Price = Guard.Against.Negative(price, nameof(price));
    IsActive = true;
    // Workers collection is initialized above
  }

  public void UpdateDetails(string name, string? description, int durationInMinutes, decimal price, string? imageUrl)
  {
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    Description = description;
    DurationInMinutes = Guard.Against.NegativeOrZero(durationInMinutes, nameof(durationInMinutes));
    Price = Guard.Against.Negative(price, nameof(price));
    ImageUrl = imageUrl;
  }

  public void SetDescription(string? description)
  {
    Description = description;
  }

  public void SetImageUrl(string? imageUrl)
  {
    ImageUrl = imageUrl;
  }

  public void Activate()
  {
    IsActive = true;
  }

  public void Deactivate()
  {
    IsActive = false;
  }
}
