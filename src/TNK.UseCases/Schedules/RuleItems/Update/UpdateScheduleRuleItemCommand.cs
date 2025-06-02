using Ardalis.Result;
using MediatR;
using System; // For Guid, TimeOnly
using TNK.UseCases.Schedules; // For ScheduleRuleItemDTO

namespace TNK.UseCases.Schedules.RuleItems.Update;

// Command to update an existing recurring rule item within a schedule.
// DayOfWeek is typically not updatable directly; if changing day, it's a delete + add.
public record UpdateScheduleRuleItemCommand(
    Guid ScheduleId,
    Guid ScheduleRuleItemId, // The ID of the specific rule item to update
    Guid WorkerId,           // For authorization context
    int BusinessProfileId,   // For authorization context
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsWorkingDay
// Note: Managing breaks (add/update/remove) within a rule item
// will likely be separate, more granular commands targeting the ScheduleRuleItemId.
) : IRequest<Result<ScheduleRuleItemDTO>>; // Returns the DTO of the updated rule item
