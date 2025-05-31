using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Infrastructure.Data; // Assuming EfRepository is in this namespace or a sub-namespace

namespace TNK.Infrastructure.Data.ServiceManagementRepositories;

public class ServiceRepository : EfRepository<Service>, IServiceRepository
{
  public ServiceRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // Implement any custom methods defined in IServiceRepository here if needed.
  // For example:
  // public async Task<List<Service>> GetActiveServicesByBusinessAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
  // {
  //     return await _dbContext.Services
  //         .Where(s => s.BusinessProfileId == businessProfileId && s.IsActive)
  //         .ToListAsync(cancellationToken);
  // }
}
