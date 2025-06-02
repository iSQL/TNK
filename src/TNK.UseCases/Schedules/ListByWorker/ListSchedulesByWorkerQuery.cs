using Ardalis.Result;
using MediatR;
using TNK.UseCases.Schedules; // For ScheduleDTO
using System; // For Guid
using System.Collections.Generic; // For List

namespace TNK.UseCases.Schedules.ListByWorker;

public record ListSchedulesByWorkerQuery(
    Guid WorkerId,
    int BusinessProfileId // For authorization (to ensure the worker belongs to the authorized business)
                          // int PageNumber = 1, // Example for pagination
                          // int PageSize = 10   // Example for pagination
) : IRequest<Result<List<ScheduleDTO>>>; // Or Result<PagedResult<ScheduleDTO>>
