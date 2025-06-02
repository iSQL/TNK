// File: TNK.UseCases/Schedules/BreakRuleDTO.cs
namespace TNK.UseCases.Schedules;

public record BreakRuleDTO(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime
);
