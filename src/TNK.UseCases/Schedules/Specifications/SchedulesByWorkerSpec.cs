using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule entity
using System; // For Guid

namespace TNK.UseCases.Schedules.Specifications;

public class SchedulesByWorkerSpec : Specification<Schedule>
{
  public SchedulesByWorkerSpec(Guid workerId)
  {
    Query
        .Where(schedule => schedule.WorkerId == workerId)
        .Include(s => s.RuleItems)
            .ThenInclude(ri => ri.Breaks)
        .Include(s => s.Overrides)
        // If Overrides can have Breaks, include them similarly:
        // .ThenInclude(o => o.Breaks)
        .OrderByDescending(schedule => schedule.IsDefault) // Show default first
        .ThenBy(schedule => schedule.Title); // Then by title
  }

  // Optional constructor for pagination (if you implement it)
  // public SchedulesByWorkerSpec(Guid workerId, int skip, int take)
  // {
  //     Query
  //         .Where(schedule => schedule.WorkerId == workerId)
  //         .Include(s => s.RuleItems)
  //             .ThenInclude(ri => ri.Breaks)
  //         .Include(s => s.Overrides)
  //         .OrderByDescending(schedule => schedule.IsDefault)
  //         .ThenBy(schedule => schedule.Title)
  //         .Skip(skip)
  //         .Take(take);
  // }
}
