// File: TNK.UseCases/Schedules/ScheduleOverrideDTO.cs
using System; // For DateOnly, TimeOnly
using System.Collections.Generic;

namespace TNK.UseCases.Schedules;

public record ScheduleOverrideDTO(
    Guid Id,
    DateOnly OverrideDate,
    string Reason,
    bool IsWorkingDay,
    TimeOnly? StartTime, // Nullable if not a working day or using default from rule
    TimeOnly? EndTime,   // Nullable
    List<BreakRuleDTO> Breaks // Assuming overrides can also have breaks, adjust if not needed
);
