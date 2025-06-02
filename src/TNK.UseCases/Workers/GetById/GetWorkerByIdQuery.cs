using Ardalis.Result;
using MediatR;
using TNK.UseCases.Workers; // For WorkerDTO

namespace TNK.UseCases.Workers.GetById;

public record GetWorkerByIdQuery(
    Guid WorkerId,
    int BusinessProfileId // For authorization purposes
) : IRequest<Result<WorkerDTO>>;
