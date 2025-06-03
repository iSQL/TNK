using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Booking, AvailabilitySlot
using TNK.Core.ServiceManagementAggregate.Enums;   // For AvailabilitySlotStatus
using TNK.UseCases.Bookings.Specifications; // For BookingByIdWithDetailsSpec (to get original slot and DTO data)
using TNK.UseCases.Bookings; // For BookingDTO
using TNK.Core.Identity; // For ApplicationUser (Customer)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.Bookings.RescheduleByVendor;

public class RescheduleBookingByVendorHandler : IRequestHandler<RescheduleBookingByVendorCommand, Result<BookingDTO>>
{
  private readonly IRepository<Booking> _bookingRepository;
  private readonly IRepository<AvailabilitySlot> _slotRepository; // For updating both old and new slots
  private readonly IValidator<RescheduleBookingByVendorCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<RescheduleBookingByVendorHandler> _logger;

  public RescheduleBookingByVendorHandler(
      IRepository<Booking> bookingRepository,
      IRepository<AvailabilitySlot> slotRepository,
      IValidator<RescheduleBookingByVendorCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<RescheduleBookingByVendorHandler> logger)
  {
    _bookingRepository = bookingRepository;
    _slotRepository = slotRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BookingDTO>> Handle(RescheduleBookingByVendorCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation(
        "Handling RescheduleBookingByVendorCommand for BookingId: {BookingId}, NewSlotId: {NewSlotId}, BusinessProfileId: {BusinessProfileId}",
        request.BookingId, request.NewAvailabilitySlotId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for RescheduleBookingByVendorCommand: {Errors}", validationResult.Errors);
      return Result<BookingDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated) return Result<BookingDTO>.Unauthorized();
    var authUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authUserBusinessProfileId == null || (authUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to reschedule booking.", request.BusinessProfileId);
      return Result<BookingDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the booking to be rescheduled, including its current slot
    var bookingSpec = new BookingByIdWithDetailsSpec(request.BookingId); // Includes Customer, Service, Worker, AvailabilitySlot
    var bookingToReschedule = await _bookingRepository.FirstOrDefaultAsync(bookingSpec, cancellationToken);

    if (bookingToReschedule == null)
    {
      return Result<BookingDTO>.NotFound($"Booking with Id {request.BookingId} not found.");
    }
    if (bookingToReschedule.BusinessProfileId != request.BusinessProfileId)
    {
      return Result<BookingDTO>.Forbidden("Booking does not belong to the specified business profile.");
    }

    // Cannot reschedule a booking that's already cancelled or completed
    if (bookingToReschedule.Status == BookingStatus.CancelledByCustomer ||
        bookingToReschedule.Status == BookingStatus.CancelledByVendor ||
        bookingToReschedule.Status == BookingStatus.Completed ||
        bookingToReschedule.Status == BookingStatus.NoShow)
    {
      return Result<BookingDTO>.Error($"Booking is already in a final state ({bookingToReschedule.Status}) and cannot be rescheduled.");
    }

    var originalSlot = bookingToReschedule.AvailabilitySlot;
    if (originalSlot == null) // Should be loaded by the spec
    {
      _logger.LogError("Original AvailabilitySlot not found for BookingId {BookingId} despite being included in spec.", request.BookingId);
      return Result<BookingDTO>.Error("Data integrity issue: Original availability slot for the booking not found.");
    }

    if (originalSlot.Id == request.NewAvailabilitySlotId)
    {
      return Result<BookingDTO>.Error("New availability slot cannot be the same as the original slot.");
    }

    // Fetch the new availability slot
    var newSlot = await _slotRepository.GetByIdAsync(request.NewAvailabilitySlotId, cancellationToken);
    if (newSlot == null)
    {
      return Result<BookingDTO>.NotFound($"New AvailabilitySlot with Id {request.NewAvailabilitySlotId} not found.");
    }

    // Validate the new slot
    if (newSlot.Status != AvailabilitySlotStatus.Available)
    {
      return Result<BookingDTO>.Error($"New slot (Id: {newSlot.Id}) is not available. Current status: {newSlot.Status}.");
    }
    if (newSlot.WorkerId != bookingToReschedule.WorkerId) // Typically reschedule with the same worker
    {
      _logger.LogWarning("Reschedule attempt to a slot with a different worker. Original Worker: {OriginalWorkerId}, New Slot's Worker: {NewWorkerId}",
         bookingToReschedule.WorkerId, newSlot.WorkerId);
      // Decide if this is allowed. For now, let's restrict it.
      return Result<BookingDTO>.Error("Rescheduling to a slot with a different worker is not currently supported via this command.");
    }
    if (newSlot.BusinessProfileId != request.BusinessProfileId)
    {
      return Result<BookingDTO>.Error("New slot does not belong to the specified business profile.");
    }


    try
    {
      // 1. Release the original slot
      originalSlot.ReleaseSlot();

      // 2. Book the new slot
      newSlot.BookSlot(bookingToReschedule.Id);

      // 3. Update the booking entity to point to the new slot and times
      bookingToReschedule.Reschedule(newSlot.Id, newSlot.StartTime, newSlot.EndTime);

      // 4. Update vendor notes if provided
      if (request.NotesByVendor != null) // Note: UpdateNotes preserves customer notes
      {
        bookingToReschedule.UpdateNotes(bookingToReschedule.NotesByCustomer, request.NotesByVendor);
      }


      await _slotRepository.UpdateAsync(originalSlot, cancellationToken);
      await _slotRepository.UpdateAsync(newSlot, cancellationToken);
      await _bookingRepository.UpdateAsync(bookingToReschedule, cancellationToken);

      // Single SaveChanges call if DbContext is shared
      await _bookingRepository.SaveChangesAsync(cancellationToken);


      // Map the updated booking to DTO
      var customer = bookingToReschedule.Customer;
      var service = bookingToReschedule.Service;
      var worker = bookingToReschedule.Worker; // Should be the same worker as newSlot.WorkerId

      var bookingDto = new BookingDTO(
          bookingToReschedule.Id,
          bookingToReschedule.BusinessProfileId,
          bookingToReschedule.CustomerId,
          customer != null ? $"{customer.FirstName} {customer.LastName}".Trim() : "N/A",
          customer?.Email,
          customer?.PhoneNumber,
          bookingToReschedule.ServiceId,
          service?.Name ?? "N/A",
          bookingToReschedule.WorkerId, // Remains the same worker
          worker != null ? $"{worker.FirstName} {worker.LastName}".Trim() : "N/A",
          bookingToReschedule.AvailabilitySlotId, // Now points to newSlot.Id
          bookingToReschedule.BookingStartTime,   // Updated to newSlot.StartTime
          bookingToReschedule.BookingEndTime,     // Updated to newSlot.EndTime
          bookingToReschedule.Status,             // Should be Rescheduled (or Confirmed, based on entity logic)
          bookingToReschedule.NotesByCustomer,
          bookingToReschedule.NotesByVendor,      // Potentially updated
          bookingToReschedule.PriceAtBooking,
          bookingToReschedule.CancellationReason, // Should be null unless cancel was part of this
          bookingToReschedule.CreatedAt,
          bookingToReschedule.UpdatedAt
      );

      _logger.LogInformation("Successfully rescheduled BookingId: {BookingId} to NewSlotId: {NewSlotId}", request.BookingId, request.NewAvailabilitySlotId);
      return Result<BookingDTO>.Success(bookingDto);
    }
    catch (InvalidOperationException opEx) // From domain entity methods
    {
      _logger.LogWarning(opEx, "Invalid operation rescheduling booking {BookingId}: {ErrorMessage}", request.BookingId, opEx.Message);
      return Result<BookingDTO>.Error(opEx.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error rescheduling booking {BookingId}: {ErrorMessage}", request.BookingId, ex.Message);
      return Result<BookingDTO>.Error($"An error occurred while rescheduling the booking: {ex.Message}");
    }
  }
}
