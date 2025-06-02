using Ardalis.Result;
using MediatR;
using System; // For Guid, DayOfWeek, TimeOnly
using TNK.UseCases.Schedules; // For ScheduleRuleItemDTO (as return type)

namespace TNK.UseCases.Schedules.RuleItems.Add;

// Command to add a new recurring rule item to an existing schedule.
public record AddScheduleRuleItemCommand(
    Guid ScheduleId,
    Guid WorkerId,          // For authorization context via schedule's worker
    int BusinessProfileId,  // For authorization context via schedule's business
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsWorkingDay
// List<CreateBreakRuleCommandModel> Breaks // Optional: If adding breaks simultaneously
) : IRequest<Result<ScheduleRuleItemDTO>>; // Returns the DTO of the newly added rule item
