using Ardalis.Result;
using MediatR;
using System; // For Guid, DateOnly, TimeOnly
using TNK.UseCases.Schedules; // For ScheduleOverrideDTO

namespace TNK.UseCases.Schedules.Overrides.Add;

public record AddScheduleOverrideCommand(
    Guid ScheduleId,
    Guid WorkerId,          // For authorization context
    int BusinessProfileId,  // For authorization context
    DateOnly OverrideDate,
    string Reason,
    bool IsWorkingDay,
    TimeOnly? StartTime,    // Nullable, required if IsWorkingDay is true
    TimeOnly? EndTime       // Nullable, required if IsWorkingDay is true
) : IRequest<Result<ScheduleOverrideDTO>>; // Returns the DTO of the newly added override
