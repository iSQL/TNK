using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using TNK.Core.BusinessAggregate; // Assuming BusinessProfileId is a Guid

namespace TNK.Core.ServiceManagementAggregate.Entities;

public class Service : EntityBase<Guid>, IAggregateRoot
{
    public int BusinessProfileId { get; private set; } // FK to BusinessProfile
    public virtual BusinessProfile? BusinessProfile { get; private set; } // Navigation property

    public string Name { get; private set; } = string.Empty; // Initialize to avoid nullability issue
    public string? Description { get; private set; }
    public int DurationInMinutes { get; private set; } // Duration of the service
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public string? ImageUrl { get; private set; } // Optional image for the service

    // Private constructor for EF Core
    private Service() { }

    public Service(int businessProfileId, string name, int durationInMinutes, decimal price)
    {
        BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        DurationInMinutes = Guard.Against.NegativeOrZero(durationInMinutes, nameof(durationInMinutes));
        Price = Guard.Against.Negative(price, nameof(price));
        IsActive = true; // Default to active
    }

    public void UpdateDetails(string name, string? description, int durationInMinutes, decimal price, string? imageUrl)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description; // Allow null or empty
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
