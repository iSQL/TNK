using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using System;

namespace TNK.UseCases.Schedules.Overrides.Remove;

public class RemoveScheduleOverrideHandler : IRequestHandler<RemoveScheduleOverrideCommand, Result>
{
  private readonly IRepository<Schedule> _scheduleRepository;
  private readonly IValidator<RemoveScheduleOverrideCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<RemoveScheduleOverrideHandler> _logger;

  public RemoveScheduleOverrideHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<RemoveScheduleOverrideCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<RemoveScheduleOverrideHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result> Handle(RemoveScheduleOverrideCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling RemoveScheduleOverrideCommand for OverrideId: {OverrideId} in ScheduleId: {ScheduleId}",
        request.ScheduleOverrideId, request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for RemoveScheduleOverrideCommand: {Errors}", validationResult.Errors);
      return Result.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to remove schedule override.", request.BusinessProfileId);
      return Result.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the parent schedule, including its overrides
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId); // This spec includes Overrides
    var schedule = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (schedule == null)
    {
      _logger.LogWarning("Parent Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result.NotFound($"Parent Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (schedule.WorkerId != request.WorkerId || schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result.Forbidden("Schedule context mismatch.");
    }

    // Use the domain method on the Schedule entity to remove the override
    bool removed = schedule.RemoveOverride(request.ScheduleOverrideId);

    if (!removed)
    {
      _logger.LogWarning("ScheduleOverride with Id {OverrideId} not found within Schedule {ScheduleId} for removal.",
          request.ScheduleOverrideId, request.ScheduleId);
      return Result.NotFound($"ScheduleOverride with Id {request.ScheduleOverrideId} not found in the specified schedule.");
    }

    try
    {
      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully removed ScheduleOverride {OverrideId} from ScheduleId: {ScheduleId}",
          request.ScheduleOverrideId, request.ScheduleId);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing override {OverrideId} from schedule {ScheduleId}: {ErrorMessage}",
          request.ScheduleOverrideId, request.ScheduleId, ex.Message);
      return Result.Error($"An error occurred while removing the schedule override: {ex.Message}");
    }
  }
}
