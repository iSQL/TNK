using Ardalis.Result;
using MediatR;
using System; // For Guid, TimeOnly
using TNK.UseCases.Schedules; // For BreakRuleDTO

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Update;

public record UpdateBreakRuleCommand(
    Guid ScheduleId,
    Guid ScheduleRuleItemId,
    Guid BreakRuleId,        // The ID of the specific break rule to update
    Guid WorkerId,           // For authorization context
    int BusinessProfileId,   // For authorization context
    string BreakName,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime
) : IRequest<Result<BreakRuleDTO>>; // Returns the DTO of the updated break rule
