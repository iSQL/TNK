using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For AvailabilitySlot, Worker
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO
using System;

namespace TNK.UseCases.AvailabilitySlots.CreateManual;

public class CreateManualAvailabilitySlotHandler : IRequestHandler<CreateManualAvailabilitySlotCommand, Result<AvailabilitySlotDTO>>
{
  private readonly IRepository<AvailabilitySlot> _slotRepository;
  private readonly IReadRepository<Worker> _workerRepository; // To verify worker ownership
  private readonly IValidator<CreateManualAvailabilitySlotCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<CreateManualAvailabilitySlotHandler> _logger;

  public CreateManualAvailabilitySlotHandler(
      IRepository<AvailabilitySlot> slotRepository,
      IReadRepository<Worker> workerRepository,
      IValidator<CreateManualAvailabilitySlotCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<CreateManualAvailabilitySlotHandler> logger)
  {
    _slotRepository = slotRepository;
    _workerRepository = workerRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<AvailabilitySlotDTO>> Handle(CreateManualAvailabilitySlotCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling CreateManualAvailabilitySlotCommand for WorkerId: {WorkerId}, StartTime: {StartTime}",
        request.WorkerId, request.StartTime);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for CreateManualAvailabilitySlotCommand: {Errors}", validationResult.Errors);
      return Result<AvailabilitySlotDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<AvailabilitySlotDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to create manual availability slot.", request.BusinessProfileId);
      return Result<AvailabilitySlotDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Verify the worker exists and belongs to the authorized BusinessProfile
    var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (worker == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found.", request.WorkerId);
      return Result<AvailabilitySlotDTO>.NotFound($"Worker with Id {request.WorkerId} not found.");
    }
    if (worker.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker {WorkerId} does not belong to the specified BusinessProfileId {BusinessProfileId}.", request.WorkerId, request.BusinessProfileId);
      return Result<AvailabilitySlotDTO>.Forbidden("Worker does not belong to the specified business profile.");
    }

    // TODO: Add collision detection for overlapping slots for the same worker.
    // This would involve querying existing slots for the worker around the requested StartTime and EndTime.
    // Example:
    // var overlappingSpec = new OverlappingSlotsSpec(request.WorkerId, request.StartTime, request.EndTime);
    // var existingOverlappingSlots = await _slotRepository.CountAsync(overlappingSpec, cancellationToken);
    // if (existingOverlappingSlots > 0)
    // {
    //     _logger.LogWarning("Cannot create manual slot for Worker {WorkerId} due to overlap: Start {StartTime}, End {EndTime}",
    //         request.WorkerId, request.StartTime, request.EndTime);
    //     return Result<AvailabilitySlotDTO>.Conflict("The requested time slot overlaps with an existing slot for this worker.");
    // }

    var newSlot = new AvailabilitySlot(
        request.WorkerId,
        request.BusinessProfileId, // Denormalized, ensure worker.BusinessProfileId matches
        request.StartTime,
        request.EndTime,
        request.Status,
        generatingScheduleId: null // This is a manually created slot
    );
    // MarkAsManuallyCreated could be a method or property on AvailabilitySlot if desired.
    // newSlot.MarkAsManuallyCreated(); 

    try
    {
      var createdSlot = await _slotRepository.AddAsync(newSlot, cancellationToken);
      await _slotRepository.SaveChangesAsync(cancellationToken);

      var slotDto = new AvailabilitySlotDTO(
          createdSlot.Id,
          createdSlot.WorkerId,
          createdSlot.BusinessProfileId,
          createdSlot.StartTime,
          createdSlot.EndTime,
          createdSlot.Status,
          createdSlot.BookingId
      );

      _logger.LogInformation("Successfully created manual AvailabilitySlot {SlotId} for WorkerId {WorkerId}", createdSlot.Id, request.WorkerId);
      return Result<AvailabilitySlotDTO>.Success(slotDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating manual availability slot for WorkerId {WorkerId}: {ErrorMessage}", request.WorkerId, ex.Message);
      return Result<AvailabilitySlotDTO>.Error($"An error occurred while creating the manual slot: {ex.Message}");
    }
  }
}
