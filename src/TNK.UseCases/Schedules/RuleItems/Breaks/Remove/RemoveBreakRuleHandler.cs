using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, ScheduleRuleItem
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using System.Linq; // For FirstOrDefault
using System;

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Remove;

public class RemoveBreakRuleHandler : IRequestHandler<RemoveBreakRuleCommand, Result>
{
  private readonly IRepository<Schedule> _scheduleRepository; // Operate on the Schedule aggregate root
  private readonly IValidator<RemoveBreakRuleCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<RemoveBreakRuleHandler> _logger;

  public RemoveBreakRuleHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<RemoveBreakRuleCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<RemoveBreakRuleHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result> Handle(RemoveBreakRuleCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling RemoveBreakRuleCommand for BreakRuleId: {BreakRuleId} in ScheduleRuleItemId: {RuleItemId}",
        request.BreakRuleId, request.ScheduleRuleItemId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for RemoveBreakRuleCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to remove break rule.", request.BusinessProfileId);
      return Result.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the parent schedule, including its rule items and their breaks
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId); // This spec includes breaks
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

    // Find the specific rule item
    var ruleItem = schedule.RuleItems.FirstOrDefault(ri => ri.Id == request.ScheduleRuleItemId);
    if (ruleItem == null)
    {
      _logger.LogWarning("ScheduleRuleItem with Id {RuleItemId} not found within Schedule {ScheduleId}.",
          request.ScheduleRuleItemId, request.ScheduleId);
      return Result.NotFound($"ScheduleRuleItem with Id {request.ScheduleRuleItemId} not found.");
    }

    // Use the domain method on ScheduleRuleItem to remove the break
    // The RemoveBreak method should find the break by ID and remove it from its own collection.
    // It should return true if found and removed, false otherwise.
    var breakFoundAndToBeRemoved = ruleItem.Breaks.Any(b => b.Id == request.BreakRuleId);

    if (!breakFoundAndToBeRemoved)
    {
      _logger.LogWarning("BreakRule with Id {BreakRuleId} not found within ScheduleRuleItem {RuleItemId} for removal.",
          request.BreakRuleId, request.ScheduleRuleItemId);
      return Result.NotFound($"BreakRule with Id {request.BreakRuleId} not found in the specified rule item.");
    }

    ruleItem.RemoveBreak(request.BreakRuleId); // Call the domain method

    try
    {
      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully removed BreakRule {BreakRuleId} from ScheduleRuleItem {RuleItemId}", request.BreakRuleId, request.ScheduleRuleItemId);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing break rule {BreakRuleId} from ScheduleRuleItem {RuleItemId}: {ErrorMessage}",
          request.BreakRuleId, request.ScheduleRuleItemId, ex.Message);
      return Result.Error($"An error occurred while removing the break rule: {ex.Message}");
    }
  }
}
