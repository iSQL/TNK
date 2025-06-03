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

namespace TNK.UseCases.Bookings.Confirm;

public class ConfirmBookingHandler : IRequestHandler<ConfirmBookingCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _repository; // Use IRepository for updates
  private readonly IValidator<ConfirmBookingCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<ConfirmBookingHandler> _logger;

  public ConfirmBookingHandler(
      IRepository<Booking> repository,
      IValidator<ConfirmBookingCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<ConfirmBookingHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ConfirmBookingCommand for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ConfirmBookingCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to confirm bookings for BusinessProfileId ({CmdBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the booking with its details to return a complete DTO and for authorization
    var spec = new BookingByIdWithDetailsSpec(request.BookingId);
    var bookingToConfirm = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (bookingToConfirm == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found for confirmation.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    // Final Authorization: Ensure the booking belongs to the claimed BusinessProfileId
    if (bookingToConfirm.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but confirmation was attempted for BusinessProfileId {CmdBusinessId}.",
          request.BookingId, bookingToConfirm.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    try
    {
      // Call the domain method to confirm the booking
      bookingToConfirm.ConfirmBooking(); // This method throws InvalidOperationException if not in correct state

      await _repository.UpdateAsync(bookingToConfirm, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      // Map to DTO to return the updated booking details
      var customer = bookingToConfirm.Customer; // ApplicationUser
      var service = bookingToConfirm.Service;
      var worker = bookingToConfirm.Worker;

      var bookingDto = new BookingDTO(
          bookingToConfirm.Id,
          bookingToConfirm.BusinessProfileId,
          bookingToConfirm.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToConfirm.ServiceId,
          service?.Name ?? "N/A",
          bookingToConfirm.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToConfirm.AvailabilitySlotId,
          bookingToConfirm.BookingStartTime,
          bookingToConfirm.BookingEndTime,
          bookingToConfirm.Status, // Should now be Confirmed
          bookingToConfirm.NotesByCustomer,
          bookingToConfirm.NotesByVendor,
          bookingToConfirm.PriceAtBooking,
          bookingToConfirm.CancellationReason,
          bookingToConfirm.CreatedAt,
          bookingToConfirm.UpdatedAt
      );

      _logger.LogInformation("Successfully confirmed Booking with Id: {BookingId}", bookingToConfirm.Id);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (InvalidOperationException opEx) // Thrown by booking.ConfirmBooking() if state is invalid
    {
      _logger.LogWarning(opEx, "Invalid operation confirming booking {BookingId}: {ErrorMessage}", request.BookingId, opEx.Message);
      return Result<BookingDTO>.Error(opEx.Message); // Return the specific domain error
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error confirming booking {BookingId}: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred while confirming the booking: {ex.Message}");
    }
  }
}
