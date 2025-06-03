using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
// Spec to get existing "fixed" slots (manual or booked) for collision detection in a UTC date range

public class FixedSlotsByWorkerInRangeSpec : Specification<AvailabilitySlot>
{
  public FixedSlotsByWorkerInRangeSpec(Guid workerId, DateTime rangeStartUtc, DateTime rangeEndUtc)
  {
    Query
        .Where(s => s.WorkerId == workerId &&
                    (s.GeneratingScheduleId == null || s.Status == AvailabilitySlotStatus.Booked) && // Manual OR Booked
                    s.EndTime > rangeStartUtc &&   // Existing slot ends after our query range starts
                    s.StartTime < rangeEndUtc);  // Existing slot starts before our query range ends (overlap condition)
  }
}
