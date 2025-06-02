// File: TNK.UseCases/Schedules/ScheduleDTO.cs
using System; // For DateOnly
using System.Collections.Generic; // For List

namespace TNK.UseCases.Schedules;

public record ScheduleDTO(
    Guid Id,
    Guid WorkerId,
    int BusinessProfileId,
    string Title,
    bool IsDefault,
    DateOnly EffectiveStartDate,
    DateOnly? EffectiveEndDate,
    string TimeZoneId,
    List<ScheduleRuleItemDTO> RuleItems,
    List<ScheduleOverrideDTO> Overrides
);
