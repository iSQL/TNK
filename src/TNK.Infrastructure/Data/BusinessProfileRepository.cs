// src/TNK.Infrastructure/Data/BusinessProfileRepository.cs
using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.BusinessAggregate; // Assuming BusinessProfile entity is in this namespace
using TNK.Core.Interfaces;       // Assuming IBusinessProfileRepository is in this namespace
//TODO: review shared kernel -> using TNK.SharedKernel.Interfaces; // For IReadRepositoryBase and IRepositoryBase if IBusinessProfileRepository inherits from them directly without Ardalis.Specification

namespace TNK.Infrastructure.Data;

/// <summary>
/// Implements the repository for the BusinessProfile aggregate.
/// It leverages the generic EfRepository for common data access operations
/// and can be extended with specific data access methods for BusinessProfiles if needed.
/// </summary>
public class BusinessProfileRepository : RepositoryBase<BusinessProfile>, IBusinessProfileRepository
{
  /// <summary>
  /// Initializes a new instance of the <see cref="BusinessProfileRepository"/> class.
  /// </summary>
  /// <param name="dbContext">The application's database context.</param>
  public BusinessProfileRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // You can add custom data access methods specific to BusinessProfile here.
  // For example:
  // public async Task<BusinessProfile?> GetBySomeCustomCriteriaAsync(string criteria, CancellationToken cancellationToken = default)
  // {
  //     return await _dbContext.BusinessProfiles
  //         .FirstOrDefaultAsync(bp => bp.SomeProperty == criteria, cancellationToken);
  // }
}
