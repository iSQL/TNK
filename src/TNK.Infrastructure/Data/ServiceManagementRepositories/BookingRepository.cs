using Ardalis.Specification.EntityFrameworkCore;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Interfaces;
using TNK.Infrastructure.Data;

namespace TNK.Infrastructure.Data.ServiceManagementRepositories;

public class BookingRepository : EfRepository<Booking>, IBookingRepository
{
  public BookingRepository(AppDbContext dbContext) : base(dbContext)
  {
  }

  // Implement any custom methods defined in IBookingRepository here if needed.
}
