using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
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

namespace TNK.UseCases.Bookings.MarkAsNoShow;

public class MarkBookingAsNoShowHandler : IRequestHandler<MarkBookingAsNoShowCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _repository;
  private readonly IValidator<MarkBookingAsNoShowCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<MarkBookingAsNoShowHandler> _logger;

  public MarkBookingAsNoShowHandler(
      IRepository<Booking> repository,
      IValidator<MarkBookingAsNoShowCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<MarkBookingAsNoShowHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(MarkBookingAsNoShowCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling MarkBookingAsNoShowCommand for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for MarkBookingAsNoShowCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized for BusinessProfileId ({CmdBusinessId}) to mark booking as no-show.",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new BookingByIdWithDetailsSpec(request.BookingId);
    var bookingToMark = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (bookingToMark == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    if (bookingToMark.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but action was attempted for BusinessProfileId {CmdBusinessId}.",
          request.BookingId, bookingToMark.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    try
    {
      bookingToMark.MarkAsNoShow(); // Domain method call

      await _repository.UpdateAsync(bookingToMark, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      // Map to DTO
      var customer = bookingToMark.Customer;
      var service = bookingToMark.Service;
      var worker = bookingToMark.Worker;

      var bookingDto = new BookingDTO(
          bookingToMark.Id,
          bookingToMark.BusinessProfileId,
          bookingToMark.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToMark.ServiceId,
          service?.Name ?? "N/A",
          bookingToMark.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToMark.AvailabilitySlotId,
          bookingToMark.BookingStartTime,
          bookingToMark.BookingEndTime,
          bookingToMark.Status, // Should now be NoShow
          bookingToMark.NotesByCustomer,
          bookingToMark.NotesByVendor,
          bookingToMark.PriceAtBooking,
          bookingToMark.CancellationReason,
          bookingToMark.CreatedAt,
          bookingToMark.UpdatedAt
      );

      _logger.LogInformation("Successfully marked Booking with Id: {BookingId} as no-show.", bookingToMark.Id);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (InvalidOperationException opEx) // Thrown by booking.MarkAsNoShow()
    {
      _logger.LogWarning(opEx, "Invalid operation marking booking {BookingId} as no-show: {ErrorMessage}", request.BookingId, opEx.Message);
      return Result<BookingDTO>.Error(opEx.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error marking booking {BookingId} as no-show: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
