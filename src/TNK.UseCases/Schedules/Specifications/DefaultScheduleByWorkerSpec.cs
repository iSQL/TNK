
// Spec to get a worker's default schedule (including details)
using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class DefaultScheduleByWorkerSpec : Specification<Schedule>, ISingleResultSpecification<Schedule>
{
  public DefaultScheduleByWorkerSpec(Guid workerId)
  {
    Query
        .Where(s => s.WorkerId == workerId && s.IsDefault)
        .Include(s => s.RuleItems)
            .ThenInclude(ri => ri.Breaks)
        .Include(s => s.Overrides);
  }
}
