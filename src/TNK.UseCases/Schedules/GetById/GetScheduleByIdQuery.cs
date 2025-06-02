using Ardalis.Result;
using MediatR;
using TNK.UseCases.Schedules; // For ScheduleDTO
using System; // For Guid

namespace TNK.UseCases.Schedules.GetById;

public record GetScheduleByIdQuery(
    Guid ScheduleId,
    int BusinessProfileId // For authorization purposes
) : IRequest<Result<ScheduleDTO>>;
