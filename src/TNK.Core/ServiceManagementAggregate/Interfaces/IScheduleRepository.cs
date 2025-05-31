using Ardalis.SharedKernel;
using TNK.Core.ServiceManagementAggregate.Entities;

namespace TNK.Core.ServiceManagementAggregate.Interfaces;

/// <summary>
/// Repository for managing Schedule entities (scheduling rules/templates).
/// </summary>
public interface IScheduleRepository : IRepositoryBase<Schedule>, IReadRepositoryBase<Schedule>
{
  // Add any custom methods specific to Schedule entities here.
  // For example:
  // Task<Schedule?> GetDefaultScheduleForWorkerAsync(Guid workerId, CancellationToken cancellationToken = default);
  // Task<List<Schedule>> GetSchedulesForWorkerAsync(Guid workerId, DateOnly effectiveDate, CancellationToken cancellationToken = default);
}
