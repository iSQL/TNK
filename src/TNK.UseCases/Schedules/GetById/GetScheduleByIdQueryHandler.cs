using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Schedule entity
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using TNK.UseCases.Schedules; // For ScheduleDTO and other DTOs
using System.Linq; // For Select and ToList

namespace TNK.UseCases.Schedules.GetById;

public class GetScheduleByIdQueryHandler : IRequestHandler<GetScheduleByIdQuery, Result<ScheduleDTO>>
{
  private readonly IReadRepository<Schedule> _repository;
  private readonly IValidator<GetScheduleByIdQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<GetScheduleByIdQueryHandler> _logger;

  public GetScheduleByIdQueryHandler(
      IReadRepository<Schedule> repository,
      IValidator<GetScheduleByIdQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<GetScheduleByIdQueryHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<ScheduleDTO>> Handle(GetScheduleByIdQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling GetScheduleByIdQuery for ScheduleId: {ScheduleId} and BusinessProfileId: {BusinessProfileId}",
        request.ScheduleId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for GetScheduleByIdQuery: {Errors}", validationResult.Errors);
      return Result<ScheduleDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to get schedule details.");
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to query schedule for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<ScheduleDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId);
    var schedule = await _repository.FirstOrDefaultAsync(spec, cancellationToken); // Use FirstOrDefaultAsync with a spec

    if (schedule == null)
    {
      _logger.LogWarning("Schedule with Id {ScheduleId} not found.", request.ScheduleId);
      return Result<ScheduleDTO>.NotFound($"Schedule with Id {request.ScheduleId} not found.");
    }

    // Final Authorization: Ensure the fetched schedule actually belongs to the claimed BusinessProfileId
    if (schedule.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Schedule (Id: {ScheduleId}) belongs to BusinessProfileId {ActualBusinessId}, but access was attempted for BusinessProfileId {QueryBusinessId}.",
          request.ScheduleId, schedule.BusinessProfileId, request.BusinessProfileId);
      return Result<ScheduleDTO>.Forbidden("Access to this schedule is not allowed for the specified business profile.");
    }

    // Manual Mapping from Entity to DTO
    var scheduleDto = new ScheduleDTO(
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
            // If ScheduleOverride entity has Breaks, map them here. Otherwise, empty list.
            // For now, assuming no Breaks collection directly on ScheduleOverride entity as per earlier design,
            // but DTO has it for flexibility.
            new List<BreakRuleDTO>()
        )).ToList() ?? new List<ScheduleOverrideDTO>()
    );

    _logger.LogInformation("Successfully retrieved Schedule with Id: {ScheduleId}", schedule.Id);
    return Result<ScheduleDTO>.Success(scheduleDto);
  }
}
