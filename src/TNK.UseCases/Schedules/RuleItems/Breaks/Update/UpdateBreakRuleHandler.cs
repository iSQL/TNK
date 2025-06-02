using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, ScheduleRuleItem, BreakRule
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using TNK.UseCases.Schedules; // For DTOs
using System.Linq; // For FirstOrDefault
using System;

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Update;

public class UpdateBreakRuleHandler : IRequestHandler<UpdateBreakRuleCommand, Result<BreakRuleDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository; // We operate on the Schedule aggregate root
  private readonly IValidator<UpdateBreakRuleCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateBreakRuleHandler> _logger;

  public UpdateBreakRuleHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<UpdateBreakRuleCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateBreakRuleHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BreakRuleDTO>> Handle(UpdateBreakRuleCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateBreakRuleCommand for BreakRuleId: {BreakRuleId} in ScheduleRuleItemId: {RuleItemId}",
        request.BreakRuleId, request.ScheduleRuleItemId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateBreakRuleCommand: {Errors}", validationResult.Errors);
      return Result<BreakRuleDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      return Result<BreakRuleDTO>.Unauthorized();
    }
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to update break rule.", request.BusinessProfileId);
      return Result<BreakRuleDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the parent schedule, including its rule items and their breaks
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId);
    var schedule = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (schedule == null)
    {
      _logger.LogWarning("Parent Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result<BreakRuleDTO>.NotFound($"Parent Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (schedule.WorkerId != request.WorkerId || schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result<BreakRuleDTO>.Forbidden("Schedule context mismatch.");
    }

    // Find the specific rule item
    var ruleItem = schedule.RuleItems.FirstOrDefault(ri => ri.Id == request.ScheduleRuleItemId);
    if (ruleItem == null)
    {
      _logger.LogWarning("ScheduleRuleItem with Id {RuleItemId} not found within Schedule {ScheduleId}.",
          request.ScheduleRuleItemId, request.ScheduleId);
      return Result<BreakRuleDTO>.NotFound($"ScheduleRuleItem with Id {request.ScheduleRuleItemId} not found.");
    }

    // Find the specific break rule to update
    var breakRuleToUpdate = ruleItem.Breaks.FirstOrDefault(b => b.Id == request.BreakRuleId);
    if (breakRuleToUpdate == null)
    {
      _logger.LogWarning("BreakRule with Id {BreakRuleId} not found within ScheduleRuleItem {RuleItemId}.",
          request.BreakRuleId, request.ScheduleRuleItemId);
      return Result<BreakRuleDTO>.NotFound($"BreakRule with Id {request.BreakRuleId} not found.");
    }

    try
    {
      // Use the domain method on BreakRule entity to update its details
      breakRuleToUpdate.UpdateDetails(request.BreakName, request.BreakStartTime, request.BreakEndTime);

      // You might need to add logic here to check for overlaps with OTHER breaks in the same ruleItem,
      // excluding the breakRuleToUpdate itself before its update.
      // The BreakRule.UpdateDetails doesn't have context of other breaks.
      // Example check (conceptual):
      // foreach (var existingBreak in ruleItem.Breaks.Where(b => b.Id != breakRuleToUpdate.Id))
      // {
      //     if (breakRuleToUpdate.StartTime < existingBreak.EndTime && breakRuleToUpdate.EndTime > existingBreak.StartTime)
      //     {
      //         throw new ArgumentException($"The updated break '{breakRuleToUpdate.Name}' overlaps with existing break '{existingBreak.Name}'.");
      //     }
      // }


      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule (aggregate root)
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      var updatedBreakDto = new BreakRuleDTO(
          breakRuleToUpdate.Id,
          breakRuleToUpdate.Name,
          breakRuleToUpdate.StartTime,
          breakRuleToUpdate.EndTime
      );

      _logger.LogInformation("Successfully updated BreakRule {BreakRuleId} in ScheduleRuleItem {RuleItemId}", request.BreakRuleId, request.ScheduleRuleItemId);
      return Result<BreakRuleDTO>.Success(updatedBreakDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic (e.g., overlapping breaks if checked)
    {
      _logger.LogWarning(argEx, "Argument error updating break rule {BreakRuleId}: {ErrorMessage}", request.BreakRuleId, argEx.Message);
      return Result<BreakRuleDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating break rule {BreakRuleId}: {ErrorMessage}", request.BreakRuleId, ex.Message);
      return Result<BreakRuleDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
