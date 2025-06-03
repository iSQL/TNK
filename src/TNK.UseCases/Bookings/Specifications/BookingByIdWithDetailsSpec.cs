using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking entity
using System; // For Guid

namespace TNK.UseCases.Bookings.Specifications;

public class BookingByIdWithDetailsSpec : Specification<Booking>, ISingleResultSpecification<Booking>
{
  public BookingByIdWithDetailsSpec(Guid bookingId)
  {
    Query
        .Where(b => b.Id == bookingId)
        .Include(b => b.Customer)  // ApplicationUser for customer details
        .Include(b => b.Service)   // Service entity for service name, etc.
        .Include(b => b.Worker)    // Worker entity for worker name, etc.
        .Include(b => b.AvailabilitySlot); // Include the slot, though most times are on Booking
  }
}
