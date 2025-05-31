using Ardalis.SharedKernel;
using TNK.Core.ServiceManagementAggregate.Entities;

namespace TNK.Core.ServiceManagementAggregate.Interfaces;

/// <summary>
/// Repository for managing Booking entities.
/// </summary>
public interface IBookingRepository : IRepositoryBase<Booking>, IReadRepositoryBase<Booking>
{
  // Add any custom methods specific to Booking entities here.
  // For example:
  // Task<List<Booking>> GetBookingsForCustomerAsync(string customerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
  // Task<List<Booking>> GetBookingsForWorkerAsync(Guid workerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
  // Task<List<Booking>> GetBookingsForBusinessAsync(Guid businessProfileId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
  // Task<Booking?> GetBookingBySlotIdAsync(Guid availabilitySlotId, CancellationToken cancellationToken = default);
}
