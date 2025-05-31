
using Ardalis.SharedKernel; // Provides IRepository<T> and IReadRepository<T>
using TNK.Core.BusinessAggregate; 

namespace TNK.Core.Interfaces;

/// <summary>
/// Defines the contract for a repository handling BusinessProfile entities.
/// It extends the generic IRepository and IReadRepository interfaces from Ardalis.SharedKernel.
/// Custom data access methods specific to BusinessProfiles can be added here.
/// </summary>
public interface IBusinessProfileRepository : IRepository<BusinessProfile>, IReadRepository<BusinessProfile>
{
  // Task<BusinessProfile?> GetByVanityUrlAsync(string vanityUrl, CancellationToken cancellationToken = default);
  // Task<List<BusinessProfile>> GetFeaturedProfilesAsync(int count, CancellationToken cancellationToken = default);
}
