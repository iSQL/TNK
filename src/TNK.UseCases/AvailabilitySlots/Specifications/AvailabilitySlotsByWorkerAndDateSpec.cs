using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For AvailabilitySlot entity
using System; // For Guid, DateTime

namespace TNK.UseCases.AvailabilitySlots.Specifications;

public class AvailabilitySlotsByWorkerAndDateSpec : Specification<AvailabilitySlot>
{
  public AvailabilitySlotsByWorkerAndDateSpec(Guid workerId, DateTime startDate, DateTime endDate)
  {
    // Ensure the date range covers whole days if only DateOnly is provided by user
    // For example, if startDate is 2023-01-15, treat it as 2023-01-15 00:00:00
    // If endDate is 2023-01-16, treat it as covering up to 2023-01-16 23:59:59.999
    // A common way is to make endDate exclusive for the next day's start if using <
    DateTime effectiveEndDate = endDate.Date.AddDays(1); // Query up to the very end of the endDate

    Query
        .Where(slot => slot.WorkerId == workerId &&
                       slot.StartTime >= startDate.Date && // Compare against the start of the startDate
                       slot.StartTime < effectiveEndDate)  // Slots starting before the day after endDate
        .OrderBy(slot => slot.StartTime);

    // If you also want to include slots that *end* within the range or *overlap* the range,
    // the Where clause would be more complex:
    // .Where(slot => slot.WorkerId == workerId &&
    //                slot.EndTime > startDate.Date &&       // Slot ends after the start of the range
    //                slot.StartTime < effectiveEndDate); // Slot starts before the end of the range
    // For simplicity, the current spec fetches slots that *start* within the date range.
  }
}
