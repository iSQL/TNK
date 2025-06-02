using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, ScheduleOverride
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using TNK.UseCases.Schedules; // For DTOs
using System.Linq; // For FirstOrDefault
using System;
using System.Collections.Generic; // For List<BreakRuleDTO>

namespace TNK.UseCases.Schedules.Overrides.Add;

public class AddScheduleOverrideHandler : IRequestHandler<AddScheduleOverrideCommand, Result<ScheduleOverrideDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository;
  private readonly IValidator<AddScheduleOverrideCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<AddScheduleOverrideHandler> _logger;

  public AddScheduleOverrideHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<AddScheduleOverrideCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<AddScheduleOverrideHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleOverrideDTO>> Handle(AddScheduleOverrideCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling AddScheduleOverrideCommand for ScheduleId: {ScheduleId}, Date: {OverrideDate}",
        request.ScheduleId, request.OverrideDate);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for AddScheduleOverrideCommand: {Errors}", validationResult.Errors);
      return Result<ScheduleOverrideDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<ScheduleOverrideDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to add schedule override.", request.BusinessProfileId);
      return Result<ScheduleOverrideDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the parent schedule, including its overrides
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId); // This spec includes Overrides
    var schedule = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (schedule == null)
    {
      _logger.LogWarning("Parent Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result<ScheduleOverrideDTO>.NotFound($"Parent Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (schedule.WorkerId != request.WorkerId || schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result<ScheduleOverrideDTO>.Forbidden("Schedule context mismatch.");
    }

    Guid newOverrideId;
    try
    {
      // Use the domain method on Schedule entity to add the override
      schedule.AddOverride(
          request.OverrideDate,
          request.Reason,
          request.IsWorkingDay,
          request.StartTime,
          request.EndTime
      );

      // Find the newly added override to get its ID.
      var newOverrideEntity = schedule.Overrides
          .FirstOrDefault(o => o.OverrideDate == request.OverrideDate && o.Reason == request.Reason); // Heuristic find

      if (newOverrideEntity == null)
      {
        _logger.LogError("Could not identify newly added ScheduleOverride in Schedule {ScheduleId}.", request.ScheduleId);
        return Result<ScheduleOverrideDTO>.Error("Failed to add override or identify the new item.");
      }
      newOverrideId = newOverrideEntity.Id;

      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      var createdOverrideDto = new ScheduleOverrideDTO(
          newOverrideId,
          request.OverrideDate,
          request.Reason,
          request.IsWorkingDay,
          request.StartTime,
          request.EndTime,
          new List<BreakRuleDTO>() // Assuming no breaks on override for now
      );

      _logger.LogInformation("Successfully added ScheduleOverride {OverrideId} to ScheduleId: {ScheduleId}", newOverrideId, request.ScheduleId);
      return Result<ScheduleOverrideDTO>.Success(createdOverrideDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic (e.g., duplicate date)
    {
      _logger.LogWarning(argEx, "Argument error adding override to schedule {ScheduleId}: {ErrorMessage}", request.ScheduleId, argEx.Message);
      return Result<ScheduleOverrideDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding override to schedule {ScheduleId}: {ErrorMessage}", request.ScheduleId, ex.Message);
      return Result<ScheduleOverrideDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
