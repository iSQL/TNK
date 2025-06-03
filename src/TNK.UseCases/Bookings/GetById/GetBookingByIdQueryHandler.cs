using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking
using TNK.UseCases.Bookings.Specifications; // For BookingByIdWithDetailsSpec
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.Core.Identity; // For ApplicationUser (Customer)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.Bookings.GetById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, Result<BookingDTO>>
{
  private readonly IReadRepository<Booking> _repository;
  private readonly IValidator<GetBookingByIdQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<GetBookingByIdQueryHandler> _logger;

  public GetBookingByIdQueryHandler(
      IReadRepository<Booking> repository,
      IValidator<GetBookingByIdQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<GetBookingByIdQueryHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling GetBookingByIdQuery for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for GetBookingByIdQuery: {Errors}", validationResult.Errors);
      return Result<BookingDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<BookingDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to view bookings for BusinessProfileId ({QueryBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new BookingByIdWithDetailsSpec(request.BookingId);
    var booking = await _repository.FirstOrDefaultAsync(spec, cancellationToken); // Use FirstOrDefaultAsync with spec

    if (booking == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    // Final Authorization: Ensure the fetched booking actually belongs to the claimed BusinessProfileId
    if (booking.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but access was attempted for BusinessProfileId {QueryBusinessId}.",
          request.BookingId, booking.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    // Map to DTO
    var customer = booking.Customer; // ApplicationUser
    var service = booking.Service;
    var worker = booking.Worker;

    var bookingDto = new BookingDTO(
        booking.Id,
        booking.BusinessProfileId,
        booking.CustomerId,
        customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
        customer?.Email,
        customer?.PhoneNumber,
        booking.ServiceId,
        service?.Name ?? "N/A",
        booking.WorkerId,
        worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
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

    _logger.LogInformation("Successfully retrieved Booking with Id: {BookingId}", booking.Id);
    return Result<BookingDTO>.Success(bookingDto);
  }
}
