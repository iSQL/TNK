using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Infrastructure.Data;

namespace TNK.Infrastructure.Data.ServiceManagementRepositories;

public class ScheduleRepository : EfRepository<Schedule>, IScheduleRepository
{
  public ScheduleRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // Implement any custom methods defined in IScheduleRepository here if needed.
}
