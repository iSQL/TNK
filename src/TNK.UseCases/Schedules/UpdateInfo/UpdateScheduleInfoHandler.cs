using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using TNK.UseCases.Schedules; // For DTOs
using System.Linq; // For mapping
using System; // For TimeZoneNotFoundException, InvalidTimeZoneException


namespace TNK.UseCases.Schedules.UpdateInfo;

public class UpdateScheduleInfoHandler : IRequestHandler<UpdateScheduleInfoCommand, Result<ScheduleDTO>>
{
  private readonly IRepository<Schedule> _repository; // Use IRepository for updates
  private readonly IValidator<UpdateScheduleInfoCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateScheduleInfoHandler> _logger;

  public UpdateScheduleInfoHandler(
      IRepository<Schedule> repository,
      IValidator<UpdateScheduleInfoCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateScheduleInfoHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleDTO>> Handle(UpdateScheduleInfoCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateScheduleInfoCommand for ScheduleId: {ScheduleId}", request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateScheduleInfoCommand: {Errors}", validationResult.Errors);
      return Result<ScheduleDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to update schedule info.");
      return Result<ScheduleDTO>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<ScheduleDTO>.Forbidden("User is not associated with a business profile.");
    }

    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin"))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to update schedule info for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<ScheduleDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Fetch the schedule with its details to ensure we have all data for the DTO later if needed,
    // and to verify ownership context (WorkerId, BusinessProfileId).
    // Using the same spec as GetById is fine, or a simpler one if not returning all details.
    // For updating, we primarily need the Schedule entity itself.
    var scheduleToUpdate = await _repository.GetByIdAsync(request.ScheduleId, cancellationToken);

    if (scheduleToUpdate == null)
    {
      _logger.LogWarning("Schedule with Id {ScheduleId} not found for update.", request.ScheduleId);
      return Result<ScheduleDTO>.NotFound($"Schedule with Id {request.ScheduleId} not found.");
    }

    // Authorization: Verify ownership
    if (scheduleToUpdate.BusinessProfileId != request.BusinessProfileId || scheduleToUpdate.WorkerId != request.WorkerId)
    {
      _logger.LogWarning("Schedule (Id: {ScheduleId}) does not belong to the specified WorkerId ({WorkerId}) or BusinessProfileId ({BusinessProfileId}).",
          request.ScheduleId, request.WorkerId, request.BusinessProfileId);
      return Result<ScheduleDTO>.Forbidden("Schedule does not match the provided worker or business profile context.");
    }

    // Verify TimeZoneId before updating (though validator also checks)
    try
    {
      TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
    }
    catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
    {
      _logger.LogWarning(ex, "Invalid TimeZoneId '{TimeZoneId}' provided in UpdateScheduleInfoCommand.", request.TimeZoneId);
      return Result<ScheduleDTO>.Error($"The TimeZoneId '{request.TimeZoneId}' is invalid.");
    }

    // TODO: Handle 'IsDefault' logic more robustly.
    // If request.IsDefault is true, and it's different from scheduleToUpdate.IsDefault,
    // you need to find other schedules for the same worker (WorkerId) that are currently default
    // and set their IsDefault to false. This requires additional repository calls.
    // This could be encapsulated in a domain service or handled here carefully.
    // Example:
    if (request.IsDefault && !scheduleToUpdate.IsDefault)
    {
      // _logger.LogInformation("Setting Schedule {ScheduleId} as default for Worker {WorkerId}. Unsetting other defaults.", request.ScheduleId, request.WorkerId);
      // var workerSchedulesSpec = new SchedulesByWorkerSpec(request.WorkerId); // Simple spec just for WorkerId
      // var otherSchedules = await _repository.ListAsync(workerSchedulesSpec, cancellationToken);
      // foreach (var otherSchedule in otherSchedules.Where(s => s.Id != request.ScheduleId && s.IsDefault))
      // {
      //     otherSchedule.SetAsDefault(false); // Assuming SetAsDefault method exists
      //     await _repository.UpdateAsync(otherSchedule, cancellationToken);
      // }
    }
    // The SetAsDefault(bool) method on the Schedule entity itself should just set the property.
    // The coordination of unsetting others is an application/domain service concern.
    // For now, we rely on the entity method call below.

    scheduleToUpdate.UpdateInfo(
        request.Title,
        request.EffectiveStartDate,
        request.EffectiveEndDate,
        request.TimeZoneId,
        request.IsDefault
    );

    try
    {
      await _repository.UpdateAsync(scheduleToUpdate, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      // It's good practice to reload the entity with all details if returning a full DTO
      // or map carefully. For now, we'll re-fetch using the spec to ensure DTO is complete.
      var updatedScheduleWithDetailsSpec = new ScheduleByIdWithDetailsSpec(scheduleToUpdate.Id);
      var updatedSchedule = await _repository.FirstOrDefaultAsync(updatedScheduleWithDetailsSpec, cancellationToken);

      if (updatedSchedule == null) // Should not happen if update was successful
      {
        _logger.LogError("Failed to re-fetch schedule {ScheduleId} after update.", scheduleToUpdate.Id);
        return Result<ScheduleDTO>.Error("Failed to retrieve updated schedule details.");
      }

      // Map to DTO
      var scheduleDto = new ScheduleDTO(
          updatedSchedule.Id,
          updatedSchedule.WorkerId,
          updatedSchedule.BusinessProfileId,
          updatedSchedule.Title,
          updatedSchedule.IsDefault,
          updatedSchedule.EffectiveStartDate,
          updatedSchedule.EffectiveEndDate,
          updatedSchedule.TimeZoneId,
          updatedSchedule.RuleItems?.Select(ri => new ScheduleRuleItemDTO(
              ri.Id,
              ri.DayOfWeek,
              ri.StartTime,
              ri.EndTime,
              ri.IsWorkingDay,
              ri.Breaks?.Select(b => new BreakRuleDTO(
                  b.Id,
                  b.Name,
                  b.StartTime,
                  b.EndTime
              )).ToList() ?? new List<BreakRuleDTO>()
          )).ToList() ?? new List<ScheduleRuleItemDTO>(),
          updatedSchedule.Overrides?.Select(o => new ScheduleOverrideDTO(
              o.Id,
              o.OverrideDate,
              o.Reason,
              o.IsWorkingDay,
              o.StartTime,
              o.EndTime,
              new List<BreakRuleDTO>() // Assuming ScheduleOverride doesn't have its own breaks for now
          )).ToList() ?? new List<ScheduleOverrideDTO>()
      );

      _logger.LogInformation("Successfully updated info for Schedule with Id: {ScheduleId}", scheduleToUpdate.Id);
      return Result<ScheduleDTO>.Success(scheduleDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating info for Schedule with Id {ScheduleId}: {ErrorMessage}", request.ScheduleId, ex.Message);
      return Result<ScheduleDTO>.Error($"An error occurred while updating the schedule info: {ex.Message}");
    }
  }
}
