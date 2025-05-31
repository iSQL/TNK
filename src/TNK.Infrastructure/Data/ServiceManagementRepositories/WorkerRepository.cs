using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Infrastructure.Data;

namespace TNK.Infrastructure.Data.ServiceManagementRepositories;

public class WorkerRepository : EfRepository<Worker>, IWorkerRepository
{
  public WorkerRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // Implement any custom methods defined in IWorkerRepository here if needed.
}
