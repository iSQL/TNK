using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking, AvailabilitySlot
using TNK.Core.ServiceManagementAggregate.Enums;   // For AvailabilitySlotStatus
using TNK.UseCases.Bookings.Specifications; // For BookingByIdWithDetailsSpec
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.Core.Identity; // For ApplicationUser (Customer)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.Bookings.CancelByVendor;

public class CancelBookingByVendorHandler : IRequestHandler<CancelBookingByVendorCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _bookingRepository;
  private readonly IRepository<AvailabilitySlot> _slotRepository; // To update the slot
  private readonly IValidator<CancelBookingByVendorCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<CancelBookingByVendorHandler> _logger;

  public CancelBookingByVendorHandler(
      IRepository<Booking> bookingRepository,
      IRepository<AvailabilitySlot> slotRepository,
      IValidator<CancelBookingByVendorCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<CancelBookingByVendorHandler> logger)
  {
    _bookingRepository = bookingRepository;
    _slotRepository = slotRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(CancelBookingByVendorCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling CancelBookingByVendorCommand for BookingId: {BookingId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for CancelBookingByVendorCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to cancel bookings for BusinessProfileId ({CmdBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the booking with its details, including the associated AvailabilitySlot
    var spec = new BookingByIdWithDetailsSpec(request.BookingId); // This spec includes Customer, Service, Worker, AvailabilitySlot
    var bookingToCancel = await _bookingRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (bookingToCancel == null)
    {
      _logger.LogWarning("Booking with Id {BookingId} not found for cancellation.", request.BookingId);
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }

    // Final Authorization: Ensure the booking belongs to the claimed BusinessProfileId
    if (bookingToCancel.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Booking (Id: {BookingId}) belongs to BusinessProfileId {ActualBusinessId}, but cancellation was attempted for BusinessProfileId {CmdBusinessId}.",
          request.BookingId, bookingToCancel.BusinessProfileId, request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("Access to this booking is not allowed for the specified business profile.");
    }

    // Fetch the linked availability slot to update it
    var slot = bookingToCancel.AvailabilitySlot; // Already loaded by BookingByIdWithDetailsSpec
    if (slot == null)
    {
      // This indicates a data integrity issue if a booking exists without a valid slot.
      _logger.LogError("Booking {BookingId} has an invalid or missing AvailabilitySlotId {SlotId}.", bookingToCancel.Id, bookingToCancel.AvailabilitySlotId);
      return Result<BookingDTO>.Error("Data inconsistency: Booking is linked to an invalid availability slot.");
    }

    try
    {
      // Call the domain method to cancel the booking
      bookingToCancel.CancelBooking(cancelledByVendor: true, request.CancellationReason);

      // Update the availability slot: make it available again and clear booking link
      // We need to ensure methods on AvailabilitySlot are suitable.
      // We defined `slot.ReleaseSlot()` or `slot.UpdateStatus(AvailabilitySlotStatus.Available); slot.ClearBookingLink();`
      // `ReleaseSlot` is more semantically correct here.
      slot.ReleaseSlot(); // This method should set Status to Available and BookingId to null.

      await _bookingRepository.UpdateAsync(bookingToCancel, cancellationToken);
      await _slotRepository.UpdateAsync(slot, cancellationToken); // Save the updated slot too

      // Ideally, SaveChangesAsync would be called once if using a Unit of Work pattern
      // or if both repositories share the same DbContext instance and SaveChanges on one cascades.
      // For now, let's assume they might be separate or need explicit saves if not handled by a UoW.
      // If _bookingRepository and _slotRepository share the same DbContext, one SaveChangesAsync is enough.
      // Let's assume they do share it (common with EF Core DI setup for IRepository<T>).
      await _bookingRepository.SaveChangesAsync(cancellationToken);


      // Map to DTO to return the updated (cancelled) booking details
      var customer = bookingToCancel.Customer;
      var service = bookingToCancel.Service;
      var worker = bookingToCancel.Worker;

      var bookingDto = new BookingDTO(
          bookingToCancel.Id,
          bookingToCancel.BusinessProfileId,
          bookingToCancel.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToCancel.ServiceId,
          service?.Name ?? "N/A",
          bookingToCancel.WorkerId,
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToCancel.AvailabilitySlotId,
          bookingToCancel.BookingStartTime,
          bookingToCancel.BookingEndTime,
          bookingToCancel.Status, // Should now be CancelledByVendor
          bookingToCancel.NotesByCustomer,
          bookingToCancel.NotesByVendor,
          bookingToCancel.PriceAtBooking,
          bookingToCancel.CancellationReason, // Should now be set
          bookingToCancel.CreatedAt,
          bookingToCancel.UpdatedAt
      );

      _logger.LogInformation("Successfully cancelled Booking with Id: {BookingId} by vendor.", bookingToCancel.Id);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (InvalidOperationException opEx) // Thrown by booking.CancelBooking() if state is invalid
    {
      _logger.LogWarning(opEx, "Invalid operation cancelling booking {BookingId}: {ErrorMessage}", request.BookingId, opEx.Message);
      return Result<BookingDTO>.Error(opEx.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error cancelling booking {BookingId}: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred while cancelling the booking: {ex.Message}");
    }
  }
}
