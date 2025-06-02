using Ardalis.Result;
using MediatR;
using TNK.UseCases.Workers; // For WorkerDTO

namespace TNK.UseCases.Workers.ListByBusiness;

public record ListWorkersByBusinessQuery(
    int BusinessProfileId
// int PageNumber = 1, // Example for pagination
// int PageSize = 10   // Example for pagination
) : IRequest<Result<List<WorkerDTO>>>; // Or Result<PagedResult<WorkerDTO>> if using pagination
