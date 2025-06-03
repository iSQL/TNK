using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking entity
using TNK.Core.ServiceManagementAggregate.Enums;   // For BookingStatus
using System;
using System.Linq.Expressions;

namespace TNK.UseCases.Bookings.Specifications;

public class BookingsByBusinessSpec : Specification<Booking>
{
  public BookingsByBusinessSpec(
      int businessProfileId,
      Guid? workerId,
      Guid? serviceId,
      string? customerId,
      BookingStatus? status,
      DateTime? dateFrom,
      DateTime? dateTo,
      int pageNumber,
      int pageSize)
  {
    // Start with the base query for the business
    Query.Where(b => b.BusinessProfileId == businessProfileId);

    // Apply filters conditionally
    if (workerId.HasValue)
    {
      Query.Where(b => b.WorkerId == workerId.Value);
    }
    if (serviceId.HasValue)
    {
      Query.Where(b => b.ServiceId == serviceId.Value);
    }
    if (!string.IsNullOrWhiteSpace(customerId))
    {
      Query.Where(b => b.CustomerId == customerId);
    }
    if (status.HasValue)
    {
      Query.Where(b => b.Status == status.Value);
    }
    if (dateFrom.HasValue)
    {
      // Assuming dateFrom is the start of the day
      Query.Where(b => b.BookingStartTime >= dateFrom.Value.Date);
    }
    if (dateTo.HasValue)
    {
      // Assuming dateTo is the start of the day, so we query up to the end of that day
      Query.Where(b => b.BookingStartTime < dateTo.Value.Date.AddDays(1));
    }

    // Include related data for the DTO
    Query.Include(b => b.Customer); // ApplicationUser
    Query.Include(b => b.Service);
    Query.Include(b => b.Worker);
    // Query.Include(b => b.AvailabilitySlot); // If needed for DTO

    // Default ordering
    Query.OrderByDescending(b => b.BookingStartTime);

    // Apply pagination
    Query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
  }

  // Constructor for counting total records matching filters (without pagination and includes for performance)
  public BookingsByBusinessSpec(
      int businessProfileId,
      Guid? workerId,
      Guid? serviceId,
      string? customerId,
      BookingStatus? status,
      DateTime? dateFrom,
      DateTime? dateTo)
  {
    Query.Where(b => b.BusinessProfileId == businessProfileId);

    if (workerId.HasValue)
    {
      Query.Where(b => b.WorkerId == workerId.Value);
    }
    if (serviceId.HasValue)
    {
      Query.Where(b => b.ServiceId == serviceId.Value);
    }
    if (!string.IsNullOrWhiteSpace(customerId))
    {
      Query.Where(b => b.CustomerId == customerId);
    }
    if (status.HasValue)
    {
      Query.Where(b => b.Status == status.Value);
    }
    if (dateFrom.HasValue)
    {
      Query.Where(b => b.BookingStartTime >= dateFrom.Value.Date);
    }
    if (dateTo.HasValue)
    {
      Query.Where(b => b.BookingStartTime < dateTo.Value.Date.AddDays(1));
    }
  }
}
