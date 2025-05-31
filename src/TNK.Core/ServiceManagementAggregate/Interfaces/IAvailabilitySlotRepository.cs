using Ardalis.SharedKernel;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;

namespace TNK.Core.ServiceManagementAggregate.Interfaces;

/// <summary>
/// Repository for managing AvailabilitySlot entities.
/// </summary>
public interface IAvailabilitySlotRepository : IRepositoryBase<AvailabilitySlot>, IReadRepositoryBase<AvailabilitySlot>
{
  // Add any custom methods specific to AvailabilitySlot entities here.
  // For example:
  // Task<List<AvailabilitySlot>> GetSlotsForWorkerByDateRangeAsync(Guid workerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
  // Task<List<AvailabilitySlot>> GetAvailableSlotsForServiceAsync(Guid serviceId, DateTime preferredDate, CancellationToken cancellationToken = default);
  // Task UpdateSlotStatusBatchAsync(IEnumerable<Guid> slotIds, AvailabilitySlotStatus newStatus, CancellationToken cancellationToken = default);
}
