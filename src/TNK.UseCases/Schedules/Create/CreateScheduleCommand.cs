using Ardalis.Result;
using MediatR;
using System; // For DateOnly

namespace TNK.UseCases.Schedules.Create;

// Command to create a new Schedule template for a worker.
// Initially, this command will create the main Schedule entity.
// Adding rule items and overrides will be handled by separate commands.
public record CreateScheduleCommand(
    Guid WorkerId,
    int BusinessProfileId, // For authorization
    string Title,
    DateOnly EffectiveStartDate,
    DateOnly? EffectiveEndDate, // Nullable for ongoing schedules
    string TimeZoneId,        // e.g., "Europe/Belgrade" from TimeZoneInfo.Id
    bool IsDefault = false
) : IRequest<Result<Guid>>; // Returns the ID of the newly created Schedule
