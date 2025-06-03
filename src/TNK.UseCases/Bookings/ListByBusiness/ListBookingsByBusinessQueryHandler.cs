using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking, ApplicationUser, Service, Worker
using TNK.UseCases.Bookings.Specifications; // For BookingsByBusinessSpec
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.UseCases.Common.Models; // For PagedResult
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TNK.Core.Identity; // For ApplicationUser

namespace TNK.UseCases.Bookings.ListByBusiness;

public class ListBookingsByBusinessQueryHandler : IRequestHandler<ListBookingsByBusinessQuery, Result<Common.Models.PagedResult<BookingDTO>>>
{
  private readonly IReadRepository<Booking> _bookingRepository;
  private readonly IValidator<ListBookingsByBusinessQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<ListBookingsByBusinessQueryHandler> _logger;

  public ListBookingsByBusinessQueryHandler(
      IReadRepository<Booking> bookingRepository,
      IValidator<ListBookingsByBusinessQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<ListBookingsByBusinessQueryHandler> logger)
  {
    _bookingRepository = bookingRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<Common.Models.PagedResult<BookingDTO>>> Handle(ListBookingsByBusinessQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ListBookingsByBusinessQuery for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ListBookingsByBusinessQuery: {Errors}", validationResult.Errors);
      return Result<Common.Models.PagedResult<BookingDTO>>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<Common.Models.PagedResult<BookingDTO>>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to list bookings.", request.BusinessProfileId);
      return Result<Common.Models.PagedResult<BookingDTO>>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new BookingsByBusinessSpec(
        request.BusinessProfileId,
        request.WorkerId,
        request.ServiceId,
        request.CustomerId,
        request.Status,
        request.DateFrom,
        request.DateTo,
        request.PageNumber,
        request.PageSize
    );

    var countSpec = new BookingsByBusinessSpec( // For counting total records matching filters
        request.BusinessProfileId,
        request.WorkerId,
        request.ServiceId,
        request.CustomerId,
        request.Status,
        request.DateFrom,
        request.DateTo
    );

    List<Booking> bookings = await _bookingRepository.ListAsync(spec, cancellationToken);
    int totalRecords = await _bookingRepository.CountAsync(countSpec, cancellationToken);

    if (bookings == null) // ListAsync should return empty list, not null
    {
      _logger.LogWarning("Booking list returned null for BusinessProfileId {BusinessProfileId}", request.BusinessProfileId);
      bookings = new List<Booking>(); // Ensure an empty list for mapping
    }

    var bookingDtos = bookings.Select(booking =>
    {
      // Safely access related entity properties, providing defaults if null
      // This assumes the includes in the spec correctly populated these navigation properties.
      var customer = booking.Customer; // This is ApplicationUser
      var service = booking.Service;
      var worker = booking.Worker;

      return new BookingDTO(
          booking.Id,
          booking.BusinessProfileId,
          booking.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A", // Handle potential null Customer
          customer?.Email,
          customer?.PhoneNumber,
          booking.ServiceId,
          service?.Name ?? "N/A", // Handle potential null Service
          booking.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A", // Handle potential null Worker
          booking.AvailabilitySlotId,
          booking.BookingStartTime,
          booking.BookingEndTime,
          booking.Status,
          booking.NotesByCustomer,
          booking.NotesByVendor,
          booking.PriceAtBooking,
          booking.CancellationReason,
          booking.CreatedAt,
          booking.UpdatedAt
      );
    }).ToList();

    var pagedResult = new Common.Models.PagedResult<BookingDTO>(bookingDtos, totalRecords, request.PageNumber, request.PageSize);

    _logger.LogInformation("Successfully retrieved {Count} bookings for BusinessProfileId: {BusinessProfileId}, Page: {PageNumber}", bookingDtos.Count, request.BusinessProfileId, request.PageNumber);
    return Result<Common.Models.PagedResult<BookingDTO>>.Success(pagedResult);
  }
}
