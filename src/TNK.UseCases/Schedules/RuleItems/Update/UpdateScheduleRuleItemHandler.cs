using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, ScheduleRuleItem
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using TNK.UseCases.Schedules; // For DTOs
using System.Linq; // For FirstOrDefault on RuleItems
using System;

namespace TNK.UseCases.Schedules.RuleItems.Update;

public class UpdateScheduleRuleItemHandler : IRequestHandler<UpdateScheduleRuleItemCommand, Result<ScheduleRuleItemDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository;
  private readonly IValidator<UpdateScheduleRuleItemCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateScheduleRuleItemHandler> _logger;

  public UpdateScheduleRuleItemHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<UpdateScheduleRuleItemCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateScheduleRuleItemHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleRuleItemDTO>> Handle(UpdateScheduleRuleItemCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateScheduleRuleItemCommand for ScheduleId: {ScheduleId}, ScheduleRuleItemId: {RuleItemId}",
        request.ScheduleId, request.ScheduleRuleItemId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateScheduleRuleItemCommand: {Errors}", validationResult.Errors);
      return Result<ScheduleRuleItemDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<ScheduleRuleItemDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to update schedule rule item.", request.BusinessProfileId);
      return Result<ScheduleRuleItemDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the parent schedule, including its rule items.
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId); // This spec includes RuleItems and their Breaks
    var schedule = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (schedule == null)
    {
      _logger.LogWarning("Parent Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.NotFound($"Parent Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (schedule.WorkerId != request.WorkerId || schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result<ScheduleRuleItemDTO>.Forbidden("Schedule context mismatch.");
    }

    // Find the specific rule item to update within the schedule's loaded RuleItems
    var ruleItemToUpdate = schedule.RuleItems.FirstOrDefault(ri => ri.Id == request.ScheduleRuleItemId);
    if (ruleItemToUpdate == null)
    {
      _logger.LogWarning("ScheduleRuleItem with Id {RuleItemId} not found within Schedule {ScheduleId}.",
          request.ScheduleRuleItemId, request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.NotFound($"ScheduleRuleItem with Id {request.ScheduleRuleItemId} not found in the specified schedule.");
    }

    // Update the properties of the rule item
    // Our ScheduleRuleItem entity does not have a dedicated update method, so we set properties directly.
    // The entity constructor or property setters should handle validation if any (e.g., StartTime < EndTime).
    // The Schedule entity's AddRuleItem method had such validation.
    // We might need to add an UpdateProperties method to ScheduleRuleItem or ensure its setters are robust.
    // For now, direct update, relying on command validation.

    // Potential conflict: If DayOfWeek was changeable and changed, we'd need to check for collisions.
    // Since we're assuming DayOfWeek is NOT changed by this command, we only update time/status.

    // Domain entity for ScheduleRuleItem:
    // public DayOfWeek DayOfWeek { get; private set; }
    // public TimeOnly StartTime { get; private set; }
    // public TimeOnly EndTime { get; private set; }
    // public bool IsWorkingDay { get; private set; }
    // It appears properties are private set. We need a method on ScheduleRuleItem to update itself.
    // Let's assume we add a method like:
    // public void UpdateDetails(TimeOnly startTime, TimeOnly endTime, bool isWorkingDay) in ScheduleRuleItem.cs
    // If not, this handler cannot update the rule item's properties as they are private set.

    // For now, let's assume ScheduleRuleItem.cs is modified to have:
    // public void UpdateDetails(TimeOnly newStartTime, TimeOnly newEndTime, bool newIsWorkingDay)
    // {
    //     if (newIsWorkingDay && newStartTime >= newEndTime)
    //     {
    //         throw new ArgumentException("For a working day, start time must be before end time.");
    //     }
    //     StartTime = newStartTime;
    //     EndTime = newEndTime;
    //     IsWorkingDay = newIsWorkingDay;
    // }

    try
    {
      // Call the assumed update method on the domain entity
      ruleItemToUpdate.UpdateDetails(request.StartTime, request.EndTime, request.IsWorkingDay);

      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule (aggregate root)
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      // Map the updated rule item to its DTO
      var updatedRuleItemDto = new ScheduleRuleItemDTO(
          ruleItemToUpdate.Id,
          ruleItemToUpdate.DayOfWeek, // DayOfWeek is not changed by this command
          ruleItemToUpdate.StartTime,
          ruleItemToUpdate.EndTime,
          ruleItemToUpdate.IsWorkingDay,
          ruleItemToUpdate.Breaks?.Select(b => new BreakRuleDTO(
              b.Id,
              b.Name,
              b.StartTime,
              b.EndTime
          )).ToList() ?? new List<BreakRuleDTO>() // Include existing breaks
      );

      _logger.LogInformation("Successfully updated ScheduleRuleItem with Id: {RuleItemId} in ScheduleId: {ScheduleId}",
          request.ScheduleRuleItemId, request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.Success(updatedRuleItemDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic
    {
      _logger.LogWarning(argEx, "Argument error updating rule item {RuleItemId}: {ErrorMessage}", request.ScheduleRuleItemId, argEx.Message);
      return Result<ScheduleRuleItemDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating rule item {RuleItemId}: {ErrorMessage}", request.ScheduleRuleItemId, ex.Message);
      return Result<ScheduleRuleItemDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
