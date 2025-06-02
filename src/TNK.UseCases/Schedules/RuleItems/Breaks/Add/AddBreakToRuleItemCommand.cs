using Ardalis.Result;
using MediatR;
using System; // For Guid, TimeOnly
using TNK.UseCases.Schedules; // For BreakRuleDTO

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Add;

public record AddBreakToRuleItemCommand(
    Guid ScheduleId,         // To locate the parent Schedule
    Guid ScheduleRuleItemId, // To locate the specific ScheduleRuleItem
    Guid WorkerId,           // For authorization context
    int BusinessProfileId,   // For authorization context
    string BreakName,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime
) : IRequest<Result<BreakRuleDTO>>; // Returns the DTO of the newly added break rule
