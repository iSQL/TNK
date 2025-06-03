using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For AvailabilitySlot
using TNK.Core.ServiceManagementAggregate.Enums; // For AvailabilitySlotStatus
using System;

namespace TNK.UseCases.AvailabilitySlots.Delete;

public class DeleteAvailabilitySlotHandler : IRequestHandler<DeleteAvailabilitySlotCommand, Result>
{
  private readonly IRepository<AvailabilitySlot> _slotRepository;
  private readonly IValidator<DeleteAvailabilitySlotCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<DeleteAvailabilitySlotHandler> _logger;

  public DeleteAvailabilitySlotHandler(
      IRepository<AvailabilitySlot> slotRepository,
      IValidator<DeleteAvailabilitySlotCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<DeleteAvailabilitySlotHandler> logger)
  {
    _slotRepository = slotRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result> Handle(DeleteAvailabilitySlotCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling DeleteAvailabilitySlotCommand for SlotId: {SlotId}", request.AvailabilitySlotId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for DeleteAvailabilitySlotCommand: {Errors}", validationResult.Errors);
      return Result.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated) return Result.Unauthorized();
    var authUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authUserBusinessProfileId == null || (authUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to delete availability slot.", request.BusinessProfileId);
      return Result.Forbidden("User is not authorized for the specified business profile.");
    }

    var slotToDelete = await _slotRepository.GetByIdAsync(request.AvailabilitySlotId, cancellationToken);
    if (slotToDelete == null)
    {
      _logger.LogWarning("AvailabilitySlot with Id {SlotId} not found for deletion.", request.AvailabilitySlotId);
      // If it's already gone, some might consider this a success for an idempotent delete.
      // However, NotFound is clearer if the intent was to delete an existing specific item.
      return Result.NotFound($"AvailabilitySlot with Id {request.AvailabilitySlotId} not found.");
    }

    // Verify ownership and context
    if (slotToDelete.BusinessProfileId != request.BusinessProfileId || slotToDelete.WorkerId != request.WorkerId)
    {
      _logger.LogWarning("AvailabilitySlot {SlotId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.AvailabilitySlotId, request.WorkerId, request.BusinessProfileId);
      return Result.Forbidden("Slot does not match the provided worker or business profile context.");
    }

    // Business Rule: Cannot delete a booked slot directly.
    if (slotToDelete.Status == AvailabilitySlotStatus.Booked)
    {
      _logger.LogWarning("Attempted to delete a booked AvailabilitySlot (Id: {SlotId}, BookingId: {BookingId}). This is not allowed.",
          slotToDelete.Id, slotToDelete.BookingId);
      return Result.Error("Booked availability slots cannot be deleted directly. Please cancel the associated booking first.");
    }

    // Optional: Add a check if slotToDelete.GeneratingScheduleId != null
    // to prevent deleting schedule-generated slots via this "manual delete" command.
    // Such slots are usually managed by regenerating or by modifying the schedule template.
    // if (slotToDelete.GeneratingScheduleId != null)
    // {
    //     _logger.LogWarning("Attempted to delete a schedule-generated AvailabilitySlot (Id: {SlotId}) via manual delete command.", slotToDelete.Id);
    //     return Result.Error("Schedule-generated slots cannot be deleted manually. Modify the schedule or regenerate slots.");
    // }

    try
    {
      await _slotRepository.DeleteAsync(slotToDelete, cancellationToken);
      await _slotRepository.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully deleted AvailabilitySlot with Id: {SlotId}", slotToDelete.Id);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting availability slot {SlotId}: {ErrorMessage}", request.AvailabilitySlotId, ex.Message);
      return Result.Error($"An error occurred while deleting the availability slot: {ex.Message}");
    }
  }
}
