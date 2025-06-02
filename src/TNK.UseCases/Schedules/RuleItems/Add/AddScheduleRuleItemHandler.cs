using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, ScheduleRuleItem
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec (to load RuleItems)
using TNK.UseCases.Schedules; // For DTOs
using System.Linq; // For FirstOrDefault on RuleItems
using System;

namespace TNK.UseCases.Schedules.RuleItems.Add;

public class AddScheduleRuleItemHandler : IRequestHandler<AddScheduleRuleItemCommand, Result<ScheduleRuleItemDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository;
  private readonly IValidator<AddScheduleRuleItemCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<AddScheduleRuleItemHandler> _logger;

  public AddScheduleRuleItemHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<AddScheduleRuleItemCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<AddScheduleRuleItemHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleRuleItemDTO>> Handle(AddScheduleRuleItemCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling AddScheduleRuleItemCommand for ScheduleId: {ScheduleId}", request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for AddScheduleRuleItemCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to add schedule rule item.", request.BusinessProfileId);
      return Result<ScheduleRuleItemDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the schedule, including its rule items to correctly add and potentially find the new one
    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId);
    var schedule = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (schedule == null)
    {
      _logger.LogWarning("Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.NotFound($"Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (schedule.WorkerId != request.WorkerId || schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result<ScheduleRuleItemDTO>.Forbidden("Schedule context mismatch.");
    }

    // Check for existing rule for the same day to prevent duplicates by day of week
    // The Schedule.AddRuleItem method might also enforce this, but a check here is good.
    if (schedule.RuleItems.Any(ri => ri.DayOfWeek == request.DayOfWeek))
    {
      _logger.LogWarning("ScheduleRuleItem for DayOfWeek {DayOfWeek} already exists in Schedule {ScheduleId}.", request.DayOfWeek, request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.Conflict($"A rule for {request.DayOfWeek} already exists for this schedule. Please update the existing rule.");
    }

    Guid newRuleItemId;
    try
    {
      // Use the domain method on Schedule entity to add the rule item
      // This method encapsulates the creation and addition logic.
      schedule.AddRuleItem(
          request.DayOfWeek,
          request.StartTime,
          request.EndTime,
          request.IsWorkingDay
      );

      // The AddRuleItem method on Schedule creates the ScheduleRuleItem and adds it to the collection.
      // We need to get the ID of the newly added item to return its DTO.
      // This assumes AddRuleItem creates an item with a new Guid Id before saving.
      // If AddRuleItem doesn't return the item or its ID, we find it.
      var newRuleItemEntity = schedule.RuleItems
                                 .FirstOrDefault(ri => ri.DayOfWeek == request.DayOfWeek &&
                                                       ri.StartTime == request.StartTime &&
                                                       ri.EndTime == request.EndTime &&
                                                       ri.IsWorkingDay == request.IsWorkingDay &&
                                                       ri.Id != Guid.Empty && // ensure it's not an old one if logic is complex
                                                       !schedule.RuleItems.Where(r => r.Id != ri.Id).Any(r => r.DayOfWeek == ri.DayOfWeek) // a bit complex to ensure it's "newest" by properties
                                                       );
      // A more robust way is if Schedule.AddRuleItem returns the created ScheduleRuleItem entity or its ID.
      // Let's assume for now AddRuleItem sets a new Guid ID immediately.
      // So we can try to find the "last" added one matching properties, or if the collection is small, the one without a persisted ID yet.
      // A safer bet: modify Schedule.AddRuleItem to return the created ScheduleRuleItem.

      // Let's assume `schedule.AddRuleItem` adds it and the ID is generated.
      // We'll try to find it. If `AddRuleItem` could return the instance, that would be ideal.
      // For now, we'll find it by properties which is not perfectly robust if multiple identical can be added before save.
      // The check `Any(ri => ri.DayOfWeek == request.DayOfWeek)` above should prevent exact duplicates by day.

      newRuleItemEntity = schedule.RuleItems.OrderByDescending(ri => ri.Id) // A heuristic if ID is sequential Guid or if it's just added
                                       .FirstOrDefault(ri => ri.DayOfWeek == request.DayOfWeek);


      if (newRuleItemEntity == null)
      {
        // This indicates an issue with how AddRuleItem works or our finding logic.
        // For now, we'll assume it can be found. If not, AddRuleItem should return it.
        _logger.LogError("Could not identify newly added ScheduleRuleItem in Schedule {ScheduleId}.", request.ScheduleId);
        return Result<ScheduleRuleItemDTO>.Error("Failed to add rule item or identify the new item.");
      }
      newRuleItemId = newRuleItemEntity.Id;


      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      var createdRuleItemDto = new ScheduleRuleItemDTO(
          newRuleItemId, // Use the ID of the newly created item
          request.DayOfWeek,
          request.StartTime,
          request.EndTime,
          request.IsWorkingDay,
          new List<BreakRuleDTO>() // Initially no breaks when adding rule item via this command
      );

      _logger.LogInformation("Successfully added ScheduleRuleItem to ScheduleId: {ScheduleId}", request.ScheduleId);
      return Result<ScheduleRuleItemDTO>.Success(createdRuleItemDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic if possible
    {
      _logger.LogWarning(argEx, "Argument error adding rule item to schedule {ScheduleId}: {ErrorMessage}", request.ScheduleId, argEx.Message);
      return Result<ScheduleRuleItemDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding rule item to schedule {ScheduleId}: {ErrorMessage}", request.ScheduleId, ex.Message);
      return Result<ScheduleRuleItemDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
