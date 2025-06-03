using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IRepository, IReadRepository
using Ardalis.Specification;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.ServiceManagementAggregate.Enums;
using TNK.UseCases.AvailabilitySlots.Specifications; // We'll need some new specs
using TNK.UseCases.Schedules.Specifications; // For ScheduleByIdWithDetailsSpec
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TNK.UseCases.AvailabilitySlots.Generate;

public class GenerateAvailabilitySlotsHandler : IRequestHandler<GenerateAvailabilitySlotsCommand, Result<int>>
{
  private readonly IRepository<AvailabilitySlot> _slotRepository;
  private readonly IReadRepository<Worker> _workerRepository;
  private readonly IReadRepository<Schedule> _scheduleRepository;
  private readonly IValidator<GenerateAvailabilitySlotsCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<GenerateAvailabilitySlotsHandler> _logger;

  // Helper class for time segments
  private record TimeSegment(DateTime Start, DateTime End);

  public GenerateAvailabilitySlotsHandler(
      IRepository<AvailabilitySlot> slotRepository,
      IReadRepository<Worker> workerRepository,
      IReadRepository<Schedule> scheduleRepository,
      IValidator<GenerateAvailabilitySlotsCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<GenerateAvailabilitySlotsHandler> logger)
  {
    _slotRepository = slotRepository;
    _workerRepository = workerRepository;
    _scheduleRepository = scheduleRepository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<int>> Handle(GenerateAvailabilitySlotsCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation(
        "Handling GenerateAvailabilitySlotsCommand for WorkerId: {WorkerId}, BusinessProfileId: {BusinessProfileId}, DateRange: {StartDate} to {EndDate}, ScheduleId: {ScheduleId}",
        request.WorkerId, request.BusinessProfileId, request.StartDate, request.EndDate, request.ScheduleId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for GenerateAvailabilitySlotsCommand: {Errors}", validationResult.Errors);
      return Result<int>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated) return Result<int>.Unauthorized();
    var authUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authUserBusinessProfileId == null || (authUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")))
    {
      return Result<int>.Forbidden("User not authorized for the specified business profile.");
    }

    var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (worker == null || worker.BusinessProfileId != request.BusinessProfileId)
    {
      return Result<int>.NotFound($"Worker with Id {request.WorkerId} not found or does not belong to BusinessProfile {request.BusinessProfileId}.");
    }

    // Fetch Schedule (either specified or default)
    Schedule? scheduleToUse;
    if (request.ScheduleId.HasValue)
    {
      var spec = new ScheduleByIdWithDetailsSpec(request.ScheduleId.Value);
      scheduleToUse = await _scheduleRepository.FirstOrDefaultAsync(spec, cancellationToken);
      if (scheduleToUse == null || scheduleToUse.WorkerId != request.WorkerId)
      {
        return Result<int>.NotFound($"Schedule with Id {request.ScheduleId} not found or does not belong to Worker {request.WorkerId}.");
      }
    }
    else // Fetch default schedule for the worker
    {
      var defaultScheduleSpec = new DefaultScheduleByWorkerSpec(request.WorkerId); // You'll need to create this spec
      scheduleToUse = await _scheduleRepository.FirstOrDefaultAsync(defaultScheduleSpec, cancellationToken);
      if (scheduleToUse == null) 
      {
        return Result<int>.NotFound($"No default schedule found for Worker {request.WorkerId}. Please specify a ScheduleId.");
      }
    }
    _logger.LogInformation("Using ScheduleId: {ActualScheduleId} for generation.", scheduleToUse.Id);


    TimeZoneInfo scheduleTimeZone;
    try
    {
      scheduleTimeZone = TimeZoneInfo.FindSystemTimeZoneById(scheduleToUse.TimeZoneId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Invalid TimeZoneId '{TimeZoneId}' in schedule {ScheduleId}.", scheduleToUse.TimeZoneId, scheduleToUse.Id);
      return Result<int>.Error($"Invalid TimeZoneId '{scheduleToUse.TimeZoneId}' in the schedule.");
    }

    // --- Step 1: Clear existing generated, unbooked slots in the date range if requested ---
    if (request.OverwriteExistingGeneratedUnbookedSlots)
    {
      // Define the full DateTime range for clearing (UTC)
      // StartDate is DateOnly, EndDate is DateOnly.
      // We need to convert these to DateTime UTC range.
      // The slots are stored in UTC.
      DateTime rangeStartUtc = TimeZoneInfo.ConvertTimeToUtc(request.StartDate.ToDateTime(TimeOnly.MinValue), scheduleTimeZone);
      DateTime rangeEndUtc = TimeZoneInfo.ConvertTimeToUtc(request.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue), scheduleTimeZone);

      var specToDelete = new UnbookedGeneratedSlotsByWorkerInRangeSpec(request.WorkerId, rangeStartUtc, rangeEndUtc);
      var slotsToDelete = await _slotRepository.ListAsync(specToDelete, cancellationToken);

      if (slotsToDelete.Any())
      {
        await _slotRepository.DeleteRangeAsync(slotsToDelete, cancellationToken);
        await _slotRepository.SaveChangesAsync(cancellationToken); // Save deletions
        _logger.LogInformation("Deleted {Count} existing generated unbooked slots for WorkerId {WorkerId} in the specified date range.", slotsToDelete.Count, request.WorkerId);
      }
    }

    // --- Step 2: Fetch existing manual or booked slots in the range to avoid collision ---
    // These slots should NOT be overwritten by generated slots.
    // The date range for fetching these should cover a bit more to catch overlaps at boundaries.
    DateTime collisionCheckStartUtc = TimeZoneInfo.ConvertTimeToUtc(request.StartDate.ToDateTime(TimeOnly.MinValue), scheduleTimeZone).AddHours(-1); // Buffer
    DateTime collisionCheckEndUtc = TimeZoneInfo.ConvertTimeToUtc(request.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue), scheduleTimeZone).AddHours(1); // Buffer

    var existingFixedSlotsSpec = new FixedSlotsByWorkerInRangeSpec(request.WorkerId, collisionCheckStartUtc, collisionCheckEndUtc);
    var fixedSlots = await _slotRepository.ListAsync(existingFixedSlotsSpec, cancellationToken);
    _logger.LogInformation("Found {Count} existing fixed (manual/booked) slots for collision checking.", fixedSlots.Count);


    var generatedSlots = new List<AvailabilitySlot>();
    var currentDate = request.StartDate;

    while (currentDate <= request.EndDate)
    {
      DayOfWeek currentDayOfWeek = currentDate.DayOfWeek;
      TimeOnly? dayStartTimeLocal = null;
      TimeOnly? dayEndTimeLocal = null;
      bool isWorkingDayForDate = false;
      ICollection<BreakRule> breaksForDay = new List<BreakRule>();

      var overrideForDate = scheduleToUse.Overrides.FirstOrDefault(o => o.OverrideDate == currentDate);

      if (overrideForDate != null)
      {
        isWorkingDayForDate = overrideForDate.IsWorkingDay;
        if (isWorkingDayForDate)
        {
          dayStartTimeLocal = overrideForDate.StartTime;
          dayEndTimeLocal = overrideForDate.EndTime;
          // Note: Our simplified ScheduleOverride does not have its own Breaks collection.
          // If it did, you would use overrideForDate.Breaks here.
          // If an override is a working day, should it inherit breaks from the standard rule?
          // For now, assume overrides define their full working period without inheriting standard breaks.
        }
        _logger.LogDebug("Date {Date}: Using override. IsWorkingDay: {IsWorking}, Start: {Start}, End: {End}", currentDate, isWorkingDayForDate, dayStartTimeLocal, dayEndTimeLocal);
      }
      else
      {
        var ruleItemForDay = scheduleToUse.RuleItems.FirstOrDefault(ri => ri.DayOfWeek == currentDayOfWeek);
        if (ruleItemForDay != null)
        {
          isWorkingDayForDate = ruleItemForDay.IsWorkingDay;
          if (isWorkingDayForDate)
          {
            dayStartTimeLocal = ruleItemForDay.StartTime;
            dayEndTimeLocal = ruleItemForDay.EndTime;
            breaksForDay = ruleItemForDay.Breaks;
          }
        }
        _logger.LogDebug("Date {Date}: Using rule item. IsWorkingDay: {IsWorking}, Start: {Start}, End: {End}, Breaks: {BreakCount}", currentDate, isWorkingDayForDate, dayStartTimeLocal, dayEndTimeLocal, breaksForDay.Count);
      }

      if (isWorkingDayForDate && dayStartTimeLocal.HasValue && dayEndTimeLocal.HasValue)
      {
        // Combine DateOnly with TimeOnly to get local DateTime
        DateTime localDayStartDateTime = currentDate.ToDateTime(dayStartTimeLocal.Value);
        DateTime localDayEndDateTime = currentDate.ToDateTime(dayEndTimeLocal.Value);

        // Create initial working segment for the day
        var workingSegments = new List<TimeSegment> { new TimeSegment(localDayStartDateTime, localDayEndDateTime) };

        // Subtract breaks from working segments (all break times are TimeOnly, local to the schedule's timezone)
        foreach (var breakRule in breaksForDay.OrderBy(b => b.StartTime))
        {
          DateTime localBreakStartDateTime = currentDate.ToDateTime(breakRule.StartTime);
          DateTime localBreakEndDateTime = currentDate.ToDateTime(breakRule.EndTime);

          var nextWorkingSegments = new List<TimeSegment>();
          foreach (var segment in workingSegments)
          {
            // If break is outside segment, keep segment
            if (localBreakEndDateTime <= segment.Start || localBreakStartDateTime >= segment.End)
            {
              nextWorkingSegments.Add(segment);
              continue;
            }
            // If break starts before segment and ends after segment starts (partial overlap start)
            // or if break is fully contained within segment
            if (localBreakStartDateTime < segment.End && localBreakEndDateTime > segment.Start)
            {
              // Add part before break
              if (localBreakStartDateTime > segment.Start)
              {
                nextWorkingSegments.Add(new TimeSegment(segment.Start, localBreakStartDateTime));
              }
              // Add part after break
              if (localBreakEndDateTime < segment.End)
              {
                nextWorkingSegments.Add(new TimeSegment(localBreakEndDateTime, segment.End));
              }
            }
            else // No overlap affecting this specific segment after prior conditions
            {
              nextWorkingSegments.Add(segment);
            }
          }
          workingSegments = nextWorkingSegments.Where(s => s.End > s.Start).ToList(); // Update with new segments
        }

        _logger.LogDebug("Date {Date}: Working segments after breaks: {SegmentCount}", currentDate, workingSegments.Count);


        // Slice remaining working segments into slots
        foreach (var segment in workingSegments)
        {
          DateTime currentSlotStartLocal = segment.Start;
          while (currentSlotStartLocal.AddMinutes(request.SlotDurationInMinutes) <= segment.End)
          {
            DateTime currentSlotEndLocal = currentSlotStartLocal.AddMinutes(request.SlotDurationInMinutes);

            // Convert slot times to UTC
            DateTime slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(currentSlotStartLocal, scheduleTimeZone);
            DateTime slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(currentSlotEndLocal, scheduleTimeZone);

            // Collision Check with existing manual/booked slots
            bool collides = fixedSlots.Any(existing =>
                slotStartUtc < existing.EndTime && slotEndUtc > existing.StartTime);

            if (!collides)
            {
              generatedSlots.Add(new AvailabilitySlot(
                  request.WorkerId,
                  worker.BusinessProfileId, // Use the actual worker's BusinessProfileId
                  slotStartUtc,
                  slotEndUtc,
                  AvailabilitySlotStatus.Available,
                  scheduleToUse.Id // Link to the schedule that generated it
              ));
            }
            else
            {
              _logger.LogDebug("Skipping generated slot {StartLocal} - {EndLocal} for Worker {WorkerId} due to collision with an existing fixed slot.", currentSlotStartLocal, currentSlotEndLocal, request.WorkerId);
            }
            currentSlotStartLocal = currentSlotEndLocal;
          }
        }
      }
      currentDate = currentDate.AddDays(1);
    }
    _logger.LogInformation("Total {Count} new slots calculated before final save.", generatedSlots.Count);

    if (generatedSlots.Any())
    {
      try
      {
        await _slotRepository.AddRangeAsync(generatedSlots, cancellationToken);
        await _slotRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully generated and saved {Count} new availability slots for WorkerId {WorkerId}.", generatedSlots.Count, request.WorkerId);
        return Result<int>.Success(generatedSlots.Count);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving generated availability slots for WorkerId {WorkerId}.", request.WorkerId);
        return Result<int>.Error($"An error occurred while saving generated slots: {ex.Message}");
      }
    }

    _logger.LogInformation("No new availability slots were generated for WorkerId {WorkerId} in the specified range or conditions.", request.WorkerId);
    return Result<int>.Success(0); // No new slots generated
  }
}
