using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore; // For DbUpdateException (if needed for specific FK issues)
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule

namespace TNK.UseCases.Schedules.Delete;

public class DeleteScheduleHandler : IRequestHandler<DeleteScheduleCommand, Result>
{
  private readonly IRepository<Schedule> _repository;
  private readonly IValidator<DeleteScheduleCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<DeleteScheduleHandler> _logger;

  public DeleteScheduleHandler(
      IRepository<Schedule> repository,
      IValidator<DeleteScheduleCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<DeleteScheduleHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result> Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling DeleteScheduleCommand for ScheduleId: {ScheduleId}", request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for DeleteScheduleCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to delete schedule.", request.BusinessProfileId);
      return Result.Forbidden("User is not authorized for the specified business profile.");
    }

    var scheduleToDelete = await _repository.GetByIdAsync(request.ScheduleId, cancellationToken);
    if (scheduleToDelete == null)
    {
      _logger.LogWarning("Schedule with Id {ScheduleId} not found for deletion.", request.ScheduleId);
      return Result.NotFound($"Schedule with Id {request.ScheduleId} not found.");
    }

    // Further Authorization: Ensure the schedule belongs to the worker and business profile in the command
    if (scheduleToDelete.WorkerId != request.WorkerId || scheduleToDelete.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule {ScheduleId} does not match worker {WorkerId} or business {BusinessProfileId} from command. Cannot delete.",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result.Forbidden("Schedule context mismatch. Deletion not allowed.");
    }

    // Business Logic Consideration:
    // When a Schedule (template) is deleted:
    // - Its ScheduleRuleItems, BreakRules, and ScheduleOverrides will be cascade deleted by the database
    //   if your EF Core configurations for these relationships (from Schedule to its children)
    //   are set to DeleteBehavior.Cascade.
    // - What about AvailabilitySlots generated from this schedule (AvailabilitySlot.GeneratingScheduleId)?
    //   In AvailabilitySlotConfiguration, we set OnDelete(DeleteBehavior.SetNull) for GeneratingScheduleId.
    //   This means deleting the schedule will set GeneratingScheduleId to null on those slots, but the slots themselves will remain.
    //   This is generally reasonable behavior. If you wanted to delete slots, the logic would be more complex.

    try
    {
      await _repository.DeleteAsync(scheduleToDelete, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully deleted Schedule with Id: {ScheduleId}", scheduleToDelete.Id);
      return Result.Success();
    }
    catch (DbUpdateException dbEx)
    {
      _logger.LogError(dbEx, "Database error while deleting schedule with Id {ScheduleId}. This could be due to unexpected FK constraints if not all child entities are set to cascade delete correctly.", request.ScheduleId);
      return Result.Error($"A database error occurred while deleting the schedule. Details: {dbEx.InnerException?.Message ?? dbEx.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting schedule with Id {ScheduleId}: {ErrorMessage}", request.ScheduleId, ex.Message);
      return Result.Error($"An error occurred while deleting the schedule: {ex.Message}");
    }
  }
}
