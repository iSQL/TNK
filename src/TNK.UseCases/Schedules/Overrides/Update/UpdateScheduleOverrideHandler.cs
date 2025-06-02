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

namespace TNK.UseCases.Schedules.Overrides.Update;

public class UpdateScheduleOverrideHandler : IRequestHandler<UpdateScheduleOverrideCommand, Result<ScheduleOverrideDTO>>
{
  private readonly IRepository<Schedule> _scheduleRepository;
  private readonly IValidator<UpdateScheduleOverrideCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateScheduleOverrideHandler> _logger;

  public UpdateScheduleOverrideHandler(
      IRepository<Schedule> scheduleRepository,
      IValidator<UpdateScheduleOverrideCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateScheduleOverrideHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleOverrideDTO>> Handle(UpdateScheduleOverrideCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateScheduleOverrideCommand for OverrideId: {OverrideId} in ScheduleId: {ScheduleId}",
        request.ScheduleOverrideId, request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateScheduleOverrideCommand: {Errors}", validationResult.Errors);
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
      _logger.LogWarning("User not authorized for BusinessProfileId {BusinessProfileId} to update schedule override.", request.BusinessProfileId);
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

    // Find the specific override to update
    var overrideToUpdate = schedule.Overrides.FirstOrDefault(o => o.Id == request.ScheduleOverrideId);
    if (overrideToUpdate == null)
    {
      _logger.LogWarning("ScheduleOverride with Id {OverrideId} not found within Schedule {ScheduleId}.",
          request.ScheduleOverrideId, request.ScheduleId);
      return Result<ScheduleOverrideDTO>.NotFound($"ScheduleOverride with Id {request.ScheduleOverrideId} not found in the specified schedule.");
    }

    // Ensure the OverrideDate from the command matches the existing one (date is not updatable here)
    if (overrideToUpdate.OverrideDate != request.OverrideDate)
    {
      _logger.LogWarning("Attempt to change OverrideDate for ScheduleOverride {OverrideId}. Current: {CurrentDate}, Requested: {RequestedDate}. This is not allowed.",
          request.ScheduleOverrideId, overrideToUpdate.OverrideDate, request.OverrideDate);
      return Result<ScheduleOverrideDTO>.Error("The OverrideDate of an existing override cannot be changed. Please delete and create a new one if the date needs to move.");
    }

    try
    {
      // Use the domain method on ScheduleOverride entity to update its details
      overrideToUpdate.UpdateDetails(
          request.Reason,
          request.IsWorkingDay,
          request.StartTime,
          request.EndTime
      );

      await _scheduleRepository.UpdateAsync(schedule, cancellationToken); // Update the parent schedule
      await _scheduleRepository.SaveChangesAsync(cancellationToken);

      var updatedOverrideDto = new ScheduleOverrideDTO(
          overrideToUpdate.Id,
          overrideToUpdate.OverrideDate, // Remains unchanged
          overrideToUpdate.Reason,
          overrideToUpdate.IsWorkingDay,
          overrideToUpdate.StartTime,
          overrideToUpdate.EndTime,
          new List<BreakRuleDTO>() // Assuming no breaks on override for now
      );

      _logger.LogInformation("Successfully updated ScheduleOverride {OverrideId} in ScheduleId: {ScheduleId}",
          request.ScheduleOverrideId, request.ScheduleId);
      return Result<ScheduleOverrideDTO>.Success(updatedOverrideDto);
    }
    catch (ArgumentException argEx) // Catch specific exceptions from domain logic
    {
      _logger.LogWarning(argEx, "Argument error updating override {OverrideId}: {ErrorMessage}", request.ScheduleOverrideId, argEx.Message);
      return Result<ScheduleOverrideDTO>.Invalid(new List<ValidationError> { new ValidationError { ErrorMessage = argEx.Message, Identifier = "DomainRule" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating override {OverrideId}: {ErrorMessage}", request.ScheduleOverrideId, ex.Message);
      return Result<ScheduleOverrideDTO>.Error($"An error occurred: {ex.Message}");
    }
  }
}
