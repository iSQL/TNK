// File: TNK.UseCases/Schedules/ScheduleRuleItemDTO.cs
using System; // For DayOfWeek
using System.Collections.Generic; // For List

namespace TNK.UseCases.Schedules;

public record ScheduleRuleItemDTO(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsWorkingDay,
    List<BreakRuleDTO> Breaks
);
