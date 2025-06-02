using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule entity
using System; // For Guid

namespace TNK.UseCases.Schedules.Specifications;

public class ScheduleByIdWithDetailsSpec : Specification<Schedule>, ISingleResultSpecification<Schedule>
{
  public ScheduleByIdWithDetailsSpec(Guid scheduleId)
  {
    Query
        .Where(s => s.Id == scheduleId)
        .Include(s => s.RuleItems)
            .ThenInclude(ri => ri.Breaks) // Include Breaks for each RuleItem
        .Include(s => s.Overrides);
    // If Overrides also have Breaks and you've implemented that relationship:
    // .ThenInclude(o => o.Breaks) // Include Breaks for each Override
  }
}
