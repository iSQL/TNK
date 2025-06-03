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

namespace TNK.UseCases.Bookings.MarkAsCompleted;

public class MarkBookingAsCompletedHandler : IRequestHandler<MarkBookingAsCompletedCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _repository;
  private readonly IValidator<MarkBookingAsCompletedCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<MarkBookingAsCompletedHandler> _logger;

  public MarkBookingAsCompletedHandler(
      IRepository<Booking> repository,
      IValidator<MarkBookingAsCompletedCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<MarkBookingAsCompletedHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(MarkBookingAsCompletedCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling MarkBookingAsCompletedCommand for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for MarkBookingAsCompletedCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized for BusinessProfileId ({CmdBusinessId}) to mark booking as completed.",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new BookingByIdWithDetailsSpec(request.BookingId);
    var bookingToComplete = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (bookingToComplete == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    if (bookingToComplete.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but action was attempted for BusinessProfileId {CmdBusinessId}.",
          request.BookingId, bookingToComplete.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    try
    {
      bookingToComplete.MarkAsCompleted(); // Domain method call

      await _repository.UpdateAsync(bookingToComplete, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      // Map to DTO
      var customer = bookingToComplete.Customer;
      var service = bookingToComplete.Service;
      var worker = bookingToComplete.Worker;

      var bookingDto = new BookingDTO(
          bookingToComplete.Id,
          bookingToComplete.BusinessProfileId,
          bookingToComplete.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToComplete.ServiceId,
          service?.Name ?? "N/A",
          bookingToComplete.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToComplete.AvailabilitySlotId,
          bookingToComplete.BookingStartTime,
          bookingToComplete.BookingEndTime,
          bookingToComplete.Status, // Should now be Completed
          bookingToComplete.NotesByCustomer,
          bookingToComplete.NotesByVendor,
          bookingToComplete.PriceAtBooking,
          bookingToComplete.CancellationReason,
          bookingToComplete.CreatedAt,
          bookingToComplete.UpdatedAt
      );

      _logger.LogInformation("Successfully marked Booking with Id: {BookingId} as completed.", bookingToComplete.Id);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (InvalidOperationException opEx) // Thrown by booking.MarkAsCompleted()
    {
      _logger.LogWarning(opEx, "Invalid operation marking booking {BookingId} as completed: {ErrorMessage}", request.BookingId, opEx.Message);
      return Result<BookingDTO>.Error(opEx.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error marking booking {BookingId} as completed: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
