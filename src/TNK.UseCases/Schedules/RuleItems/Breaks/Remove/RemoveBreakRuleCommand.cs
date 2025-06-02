using Ardalis.Result;
using MediatR;
using System; // For Guid

namespace TNK.UseCases.Schedules.RuleItems.Breaks.Remove;

public record RemoveBreakRuleCommand(
    Guid ScheduleId,
    Guid ScheduleRuleItemId,
    Guid BreakRuleId,        // The ID of the specific break rule to remove
    Guid WorkerId,           // For authorization context
    int BusinessProfileId   // For authorization context
) : IRequest<Result>; // Returns a simple Result (success/failure)
