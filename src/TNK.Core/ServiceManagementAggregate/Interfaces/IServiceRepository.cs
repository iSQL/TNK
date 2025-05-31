using Ardalis.SharedKernel;
using TNK.Core.ServiceManagementAggregate.Entities;

namespace TNK.Core.ServiceManagementAggregate.Interfaces;

/// <summary>
/// Repository for managing Service entities.
/// </summary>
public interface IServiceRepository : IRepositoryBase<Service>, IReadRepositoryBase<Service>
{
  // Add any custom methods specific to Service entities here if needed in the future.
  // For example:
  // Task<List<Service>> GetActiveServicesByBusinessAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
}
