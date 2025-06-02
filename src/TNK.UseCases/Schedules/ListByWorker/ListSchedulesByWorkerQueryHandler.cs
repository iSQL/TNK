using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule, Worker
using TNK.UseCases.Schedules.Specifications; // For SchedulesByWorkerSpec
using TNK.UseCases.Schedules; // For DTOs
using System.Collections.Generic; // For List
using System.Linq; // For Select, ToList
using System; // For Guid

namespace TNK.UseCases.Schedules.ListByWorker;

public class ListSchedulesByWorkerQueryHandler : IRequestHandler<ListSchedulesByWorkerQuery, Result<List<ScheduleDTO>>>
{
  private readonly IReadRepository<Schedule> _scheduleRepository;
  private readonly IReadRepository<Worker> _workerRepository; // To verify worker ownership
  private readonly IValidator<ListSchedulesByWorkerQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<ListSchedulesByWorkerQueryHandler> _logger;

  public ListSchedulesByWorkerQueryHandler(
      IReadRepository<Schedule> scheduleRepository,
      IReadRepository<Worker> workerRepository,
      IValidator<ListSchedulesByWorkerQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<ListSchedulesByWorkerQueryHandler> logger)
  {
    _scheduleRepository = scheduleRepository;
    _workerRepository = workerRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<List<ScheduleDTO>>> Handle(ListSchedulesByWorkerQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ListSchedulesByWorkerQuery for WorkerId: {WorkerId}, BusinessProfileId: {BusinessProfileId}",
        request.WorkerId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ListSchedulesByWorkerQuery: {Errors}", validationResult.Errors);
      return Result<List<ScheduleDTO>>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to list schedules.");
      return Result<List<ScheduleDTO>>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<List<ScheduleDTO>>.Forbidden("User is not associated with a business profile.");
    }

    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin"))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to list schedules for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<List<ScheduleDTO>>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Verify the worker belongs to the authorized BusinessProfile
    var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (worker == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found.", request.WorkerId);
      // Or you could return an empty list if the worker doesn't exist, depending on desired behavior.
      // NotFound makes it clear the primary resource for the query (worker) is missing.
      return Result<List<ScheduleDTO>>.NotFound($"Worker with Id {request.WorkerId} not found.");
    }
    if (worker.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker {WorkerId} does not belong to the specified BusinessProfileId {BusinessProfileId}.", request.WorkerId, request.BusinessProfileId);
      return Result<List<ScheduleDTO>>.Forbidden("Worker does not belong to the specified business profile.");
    }

    var spec = new SchedulesByWorkerSpec(request.WorkerId);
    // If implementing pagination:
    // var spec = new SchedulesByWorkerSpec(request.WorkerId, (request.PageNumber - 1) * request.PageSize, request.PageSize);
    // var totalRecords = await _scheduleRepository.CountAsync(new SchedulesByWorkerSpec(request.WorkerId), cancellationToken); // Need a spec without includes for count

    var schedules = await _scheduleRepository.ListAsync(spec, cancellationToken);

    if (schedules == null) // ListAsync should return empty list, not null
    {
      _logger.LogWarning("Schedule list returned null for WorkerId {WorkerId}", request.WorkerId);
      return Result<List<ScheduleDTO>>.Success(new List<ScheduleDTO>());
    }

    var scheduleDtos = schedules.Select(schedule => new ScheduleDTO(
        schedule.Id,
        schedule.WorkerId,
        schedule.BusinessProfileId,
        schedule.Title,
        schedule.IsDefault,
        schedule.EffectiveStartDate,
        schedule.EffectiveEndDate,
        schedule.TimeZoneId,
        schedule.RuleItems?.Select(ri => new ScheduleRuleItemDTO(
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
        schedule.Overrides?.Select(o => new ScheduleOverrideDTO(
            o.Id,
            o.OverrideDate,
            o.Reason,
            o.IsWorkingDay,
            o.StartTime,
            o.EndTime,
            new List<BreakRuleDTO>() // Assuming ScheduleOverride doesn't have its own breaks for now
        )).ToList() ?? new List<ScheduleOverrideDTO>()
    )).ToList();

    // If implementing pagination:
    // var pagedResult = new PagedResult<ScheduleDTO>(scheduleDtos, totalRecords, request.PageNumber, request.PageSize);
    // _logger.LogInformation("Successfully retrieved {Count} schedules for WorkerId: {WorkerId}, Page: {PageNumber}", scheduleDtos.Count, request.WorkerId, request.PageNumber);
    // return Result<PagedResult<ScheduleDTO>>.Success(pagedResult);

    _logger.LogInformation("Successfully retrieved {Count} schedules for WorkerId: {WorkerId}", scheduleDtos.Count, request.WorkerId);
    return Result<List<ScheduleDTO>>.Success(scheduleDtos);
  }
}
