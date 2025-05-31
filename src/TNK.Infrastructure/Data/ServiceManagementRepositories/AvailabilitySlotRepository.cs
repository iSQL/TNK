using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Infrastructure.Data;

namespace TNK.Infrastructure.Data.ServiceManagementRepositories;

public class AvailabilitySlotRepository : EfRepository<AvailabilitySlot>, IAvailabilitySlotRepository
{
  public AvailabilitySlotRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // Implement any custom methods defined in IAvailabilitySlotRepository here if needed.
}
