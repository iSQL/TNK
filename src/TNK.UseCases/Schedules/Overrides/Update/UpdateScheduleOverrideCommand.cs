using Ardalis.Result;
using MediatR;
using System; // For Guid, DateOnly, TimeOnly
using TNK.UseCases.Schedules; // For ScheduleOverrideDTO

namespace TNK.UseCases.Schedules.Overrides.Update;

public record UpdateScheduleOverrideCommand(
    Guid ScheduleId,
    Guid ScheduleOverrideId, // The ID of the specific override to update
    Guid WorkerId,           // For authorization context
    int BusinessProfileId,   // For authorization context
    DateOnly OverrideDate,   // Included for context/validation, but not changed by this command
    string Reason,
    bool IsWorkingDay,
    TimeOnly? StartTime,     // Nullable, required if IsWorkingDay is true
    TimeOnly? EndTime        // Nullable, required if IsWorkingDay is true
) : IRequest<Result<ScheduleOverrideDTO>>; // Returns the DTO of the updated override
