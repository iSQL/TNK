using Ardalis.Result;
using MediatR;
using System; // For Guid, DateOnly
using TNK.UseCases.Schedules; // For ScheduleDTO

namespace TNK.UseCases.Schedules.UpdateInfo;

public record UpdateScheduleInfoCommand(
    Guid ScheduleId,
    Guid WorkerId,          // To ensure context and for authorization checks
    int BusinessProfileId,  // For authorization checks
    string Title,
    DateOnly EffectiveStartDate,
    DateOnly? EffectiveEndDate,
    string TimeZoneId,
    bool IsDefault
) : IRequest<Result<ScheduleDTO>>; // Returns the updated ScheduleDTO
