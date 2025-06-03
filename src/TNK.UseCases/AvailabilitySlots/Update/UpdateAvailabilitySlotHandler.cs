using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;
using TNK.UseCases.AvailabilitySlots.Specifications; // For potential OverlappingSlotsSpec
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.AvailabilitySlots.Update;

public class UpdateAvailabilitySlotHandler : IRequestHandler<UpdateAvailabilitySlotCommand, Result<AvailabilitySlotDTO>>
{
  private readonly IRepository<AvailabilitySlot> _slotRepository;
  private readonly IRepository<Booking> _bookingRepository; // To update associated booking
  private readonly IReadRepository<Worker> _workerRepository;
  private readonly IValidator<UpdateAvailabilitySlotCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateAvailabilitySlotHandler> _logger;

  public UpdateAvailabilitySlotHandler(
      IRepository<AvailabilitySlot> slotRepository,
      IRepository<Booking> bookingRepository,
      IReadRepository<Worker> workerRepository,
      IValidator<UpdateAvailabilitySlotCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateAvailabilitySlotHandler> logger)
  {
    _slotRepository = slotRepository;
    _bookingRepository = bookingRepository;
    _workerRepository = workerRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<AvailabilitySlotDTO>> Handle(UpdateAvailabilitySlotCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateAvailabilitySlotCommand for SlotId: {SlotId}", request.AvailabilitySlotId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateAvailabilitySlotCommand: {Errors}", validationResult.Errors);
      return Result<AvailabilitySlotDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated) return Result<AvailabilitySlotDTO>.Unauthorized();
    var authUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authUserBusinessProfileId == null || (authUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      return Result<AvailabilitySlotDTO>.Forbidden("User not authorized for the specified business profile.");
    }

    var slotToUpdate = await _slotRepository.GetByIdAsync(request.AvailabilitySlotId, cancellationToken);
    if (slotToUpdate == null)
    {
      return Result<AvailabilitySlotDTO>.NotFound($"AvailabilitySlot with Id {request.AvailabilitySlotId} not found.");
    }

    // Verify ownership and context
    if (slotToUpdate.BusinessProfileId != request.BusinessProfileId || slotToUpdate.WorkerId != request.WorkerId)
    {
      return Result<AvailabilitySlotDTO>.Forbidden("Slot does not match the provided worker or business profile context.");
    }

    bool timeChanged = request.NewStartTime.HasValue && request.NewEndTime.HasValue;
    bool statusChanged = request.NewStatus.HasValue && slotToUpdate.Status != request.NewStatus.Value;

    Booking? associatedBooking = null;
    if (slotToUpdate.Status == AvailabilitySlotStatus.Booked && slotToUpdate.BookingId.HasValue)
    {
      associatedBooking = await _bookingRepository.GetByIdAsync(slotToUpdate.BookingId.Value, cancellationToken);
      if (associatedBooking == null)
      {
        // This indicates a data integrity issue if a slot is Booked but booking is missing.
        _logger.LogError("Slot {SlotId} is Booked but associated Booking {BookingId} not found.", slotToUpdate.Id, slotToUpdate.BookingId);
        return Result<AvailabilitySlotDTO>.Error("Data inconsistency: Associated booking not found for a booked slot.");
      }
    }

    // Handle Time Update
    if (timeChanged)
    {
      DateTime newStartTime = request.NewStartTime!.Value;
      DateTime newEndTime = request.NewEndTime!.Value;

      // TODO: Collision Detection for the new time slot (excluding the current slot itself).
      // var overlappingSpec = new OverlappingSlotsSpec(slotToUpdate.WorkerId, newStartTime, newEndTime, slotToUpdate.Id);
      // if (await _slotRepository.AnyAsync(overlappingSpec, cancellationToken))
      // {
      //     return Result<AvailabilitySlotDTO>.Conflict("The new time for the slot overlaps with another existing slot for this worker.");
      // }

      slotToUpdate.UpdateTime(newStartTime, newEndTime, isRescheduleOfBookedSlot: associatedBooking != null);

      if (associatedBooking != null)
      {
        // Update the associated booking's times
        // Assuming Booking entity has a method like UpdateTimes or direct setters + validation
        associatedBooking.UpdateTimes(newStartTime, newEndTime); // You'll need to add UpdateTimes to Booking entity
                                                                 // Or: associatedBooking.BookingStartTime = newStartTime; associatedBooking.BookingEndTime = newEndTime;
                                                                 // followed by validation in booking.
                                                                 // TODO: Consider raising a domain event: BookingRescheduledEvent(associatedBooking.Id)
      }
    }

    // Handle Status Update
    if (statusChanged)
    {
      var newStatus = request.NewStatus!.Value;

      if (slotToUpdate.Status == AvailabilitySlotStatus.Booked && newStatus != AvailabilitySlotStatus.Booked)
      {
        // Changing status FROM Booked to something else (e.g., Available, Unavailable by vendor cancellation)
        if (associatedBooking == null) { /* Should not happen if status was Booked */ }

        // This implies booking is being cancelled or significantly altered.
        // The booking status should be updated accordingly (e.g., CancelledByVendor).
        // For simplicity here, we are just changing slot status. A full booking cancellation flow is separate.
        // The slot.UpdateStatus will just change the status. We need to handle BookingId.
        _logger.LogInformation("Slot {SlotId} was Booked (BookingId: {BookingId}). Changing status to {NewStatus}. BookingId will be cleared.", slotToUpdate.Id, slotToUpdate.BookingId, newStatus);
        slotToUpdate.UpdateStatus(newStatus);
        slotToUpdate.ClearBookingLink(); // Assumes a method like: public void ClearBookingLink() { BookingId = null; }
      }
      else if (newStatus == AvailabilitySlotStatus.Available && slotToUpdate.BookingId != null)
      {
        // If making a previously booked slot available (e.g., customer cancelled, now vendor makes it open)
        slotToUpdate.UpdateStatus(newStatus);
        slotToUpdate.ClearBookingLink();
      }
      else
      {
        slotToUpdate.UpdateStatus(newStatus);
      }
    }

    try
    {
      await _slotRepository.UpdateAsync(slotToUpdate, cancellationToken);
      if (associatedBooking != null && timeChanged) // Only save booking if its times were changed
      {
        await _bookingRepository.UpdateAsync(associatedBooking, cancellationToken);
      }
      await _slotRepository.SaveChangesAsync(cancellationToken); // Or a shared Unit of Work SaveChangesAsync

      var updatedDto = new AvailabilitySlotDTO(
          slotToUpdate.Id,
          slotToUpdate.WorkerId,
          slotToUpdate.BusinessProfileId,
          slotToUpdate.StartTime,
          slotToUpdate.EndTime,
          slotToUpdate.Status,
          slotToUpdate.BookingId
      );
      return Result<AvailabilitySlotDTO>.Success(updatedDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating availability slot {SlotId}: {ErrorMessage}", request.AvailabilitySlotId, ex.Message);
      return Result<AvailabilitySlotDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
