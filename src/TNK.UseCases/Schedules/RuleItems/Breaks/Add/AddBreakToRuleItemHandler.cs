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

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Add;

public class AddBreakToRuleItemHandler : IRequestHandler<AddBreakToRuleItemCommand, Result<BreakRuleDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository; // We operate on the Schedule aggregate root
  private readonly IValidator<AddBreakToRuleItemCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<AddBreakToRuleItemHandler> _logger;

  public AddBreakToRuleItemHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<AddBreakToRuleItemCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<AddBreakToRuleItemHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<BreakRuleDTO>> Handle(AddBreakToRuleItemCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling AddBreakToRuleItemCommand for ScheduleRuleItemId: {RuleItemId} in ScheduleId: {ScheduleId}",
        request.ScheduleRuleItemId, request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for AddBreakToRuleItemCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to add break to schedule rule item.", request.BusinessProfileId);
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

    // Find the specific rule item within the schedule
    var ruleItem = schedule.RuleItems.FirstOrDefault(ri => ri.Id == request.ScheduleRuleItemId);
    if (ruleItem == null)
    {
      _logger.LogWarning("ScheduleRuleItem with Id {RuleItemId} not found within Schedule {ScheduleId}.",
          request.ScheduleRuleItemId, request.ScheduleId);
      return Result<BreakRuleDTO>.NotFound($"ScheduleRuleItem with Id {request.ScheduleRuleItemId} not found in the specified schedule.");
    }

    Guid newBreakRuleId;
    try
    {
      // Use the domain method on ScheduleRuleItem entity to add the break
      ruleItem.AddBreak(request.BreakName, request.BreakStartTime, request.BreakEndTime);

      // Find the newly added break to get its ID.
      // This relies on AddBreak creating the BreakRule and adding it to the collection.
      var newBreakEntity = ruleItem.Breaks
          .FirstOrDefault(b => b.Name == request.BreakName &&
                               b.StartTime == request.BreakStartTime &&
                               b.EndTime == request.BreakEndTime);
      // This find logic is a bit heuristic. A more robust way is if AddBreak returns the new BreakRule.
      // Or if the ID is generated client-side for the command (less common for new entities).

      if (newBreakEntity == null)
      {
        _logger.LogError("Could not identify newly added BreakRule to ScheduleRuleItem {RuleItemId}.", request.ScheduleRuleItemId);
        return Result<BreakRuleDTO>.Error("Failed to add break or identify the new break item.");
      }
      newBreakRuleId = newBreakEntity.Id;

      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule (aggregate root)
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      var createdBreakDto = new BreakRuleDTO(
          newBreakRuleId,
          request.BreakName,
          request.BreakStartTime,
          request.BreakEndTime
      );

      _logger.LogInformation("Successfully added BreakRule {BreakRuleId} to ScheduleRuleItem {RuleItemId}", newBreakRuleId, request.ScheduleRuleItemId);
      return Result<BreakRuleDTO>.Success(createdBreakDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic (e.g., overlapping breaks)
    {
      _logger.LogWarning(argEx, "Argument error adding break to rule item {RuleItemId}: {ErrorMessage}", request.ScheduleRuleItemId, argEx.Message);
      return Result<BreakRuleDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (InvalidOperationException opEx) // E.g., trying to add break to non-working day
    {
      _logger.LogWarning(opEx, "Operation error adding break to rule item {RuleItemId}: {ErrorMessage}", request.ScheduleRuleItemId, opEx.Message);
      return Result<BreakRuleDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = opEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding break to rule item {RuleItemId}: {ErrorMessage}", request.ScheduleRuleItemId, ex.Message);
      return Result<BreakRuleDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
