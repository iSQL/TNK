﻿// Spec to get unbooked slots generated by any schedule for a worker in a UTC date range
using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class UnbookedGeneratedSlotsByWorkerInRangeSpec : Specification<AvailabilitySlot>
{
  public UnbookedGeneratedSlotsByWorkerInRangeSpec(Guid workerId, DateTime rangeStartUtc, DateTime rangeEndUtc)
  {
    Query
        .Where(s => s.WorkerId == workerId &&
                    s.GeneratingScheduleId != null && // Was generated by a schedule
                    s.Status != AvailabilitySlotStatus.Booked && // Is not booked
                    s.StartTime >= rangeStartUtc &&
                    s.StartTime < rangeEndUtc); // Slots starting within the UTC range
  }
}
