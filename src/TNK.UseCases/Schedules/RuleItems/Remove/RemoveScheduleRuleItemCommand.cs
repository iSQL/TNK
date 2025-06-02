using Ardalis.Result;
using MediatR;
using System; // For Guid

namespace TNK.UseCases.Schedules.RuleItems.Remove;

public record RemoveScheduleRuleItemCommand(
    Guid ScheduleId,
    Guid ScheduleRuleItemId, // The ID of the specific rule item to remove
    Guid WorkerId,           // For authorization context
    int BusinessProfileId   // For authorization context
) : IRequest<Result>; // Returns a simple Result (success/failure)
