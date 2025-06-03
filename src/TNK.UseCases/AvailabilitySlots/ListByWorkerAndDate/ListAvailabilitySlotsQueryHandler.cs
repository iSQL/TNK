using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For AvailabilitySlot, Worker
using TNK.UseCases.AvailabilitySlots.Specifications; // For the spec
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO
using System.Collections.Generic;
using System.Linq;
using System;

namespace TNK.UseCases.AvailabilitySlots.ListByWorkerAndDate;

public class ListAvailabilitySlotsQueryHandler : IRequestHandler<ListAvailabilitySlotsQuery, Result<List<AvailabilitySlotDTO>>>
{
  private readonly IReadRepository<AvailabilitySlot> _slotRepository;
  private readonly IReadRepository<Worker> _workerRepository; // To verify worker ownership
  private readonly IValidator<ListAvailabilitySlotsQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<ListAvailabilitySlotsQueryHandler> _logger;

  public ListAvailabilitySlotsQueryHandler(
      IReadRepository<AvailabilitySlot> slotRepository,
      IReadRepository<Worker> workerRepository,
      IValidator<ListAvailabilitySlotsQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<ListAvailabilitySlotsQueryHandler> logger)
  {
    _slotRepository = slotRepository;
    _workerRepository = workerRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<List<AvailabilitySlotDTO>>> Handle(ListAvailabilitySlotsQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ListAvailabilitySlotsQuery for WorkerId: {WorkerId}, DateRange: {StartDate} to {EndDate}",
        request.WorkerId, request.StartDate, request.EndDate);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ListAvailabilitySlotsQuery: {Errors}", validationResult.Errors);
      return Result<List<AvailabilitySlotDTO>>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<List<AvailabilitySlotDTO>>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to list availability slots.", request.BusinessProfileId);
      return Result<List<AvailabilitySlotDTO>>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Verify the worker belongs to the authorized BusinessProfile
    var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (worker == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found.", request.WorkerId);
      return Result<List<AvailabilitySlotDTO>>.NotFound($"Worker with Id {request.WorkerId} not found.");
    }
    if (worker.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker {WorkerId} does not belong to the specified BusinessProfileId {BusinessProfileId}.", request.WorkerId, request.BusinessProfileId);
      return Result<List<AvailabilitySlotDTO>>.Forbidden("Worker does not belong to the specified business profile.");
    }

    var spec = new AvailabilitySlotsByWorkerAndDateSpec(request.WorkerId, request.StartDate, request.EndDate);
    var slots = await _slotRepository.ListAsync(spec, cancellationToken);

    if (slots == null) // ListAsync should return empty list, not null
    {
      _logger.LogWarning("Availability slot list returned null for WorkerId {WorkerId} and date range.", request.WorkerId);
      return Result<List<AvailabilitySlotDTO>>.Success(new List<AvailabilitySlotDTO>()); // Return empty list
    }

    var slotDtos = slots.Select(slot => new AvailabilitySlotDTO(
            slot.Id,
            slot.WorkerId,
            slot.BusinessProfileId,
            slot.StartTime,
            slot.EndTime,
            slot.Status,
            slot.BookingId
        )).ToList();

    _logger.LogInformation("Successfully retrieved {Count} availability slots for WorkerId {WorkerId} in date range.", slotDtos.Count, request.WorkerId);
    return Result<List<AvailabilitySlotDTO>>.Success(slotDtos);
  }
}
