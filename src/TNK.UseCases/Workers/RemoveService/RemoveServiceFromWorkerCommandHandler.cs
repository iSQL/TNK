using Ardalis.Result;
using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.Interfaces; // For IRepository

// It's good practice to have a specific specification for loading Worker with its Services
// e.g., using TNK.UseCases.Workers.Specifications;

namespace TNK.UseCases.Workers.RemoveService;

public class RemoveServiceFromWorkerCommandHandler : IRequestHandler<RemoveServiceFromWorkerCommand, Result>
{
  private readonly IRepository<Worker> _workerRepository;
  // No need to fetch Service entity if just removing by ID, but fetching ensures it belongs to the business.
  private readonly IReadRepository<Service> _serviceRepository;
  private readonly ILogger<RemoveServiceFromWorkerCommandHandler> _logger;

  public RemoveServiceFromWorkerCommandHandler(
      IRepository<Worker> workerRepository,
      IReadRepository<Service> serviceRepository,
      ILogger<RemoveServiceFromWorkerCommandHandler> logger)
  {
    _workerRepository = workerRepository;
    _serviceRepository = serviceRepository;
    _logger = logger;
  }

  public async Task<Result> Handle(RemoveServiceFromWorkerCommand request, CancellationToken cancellationToken)
  {
    try
    {
      // IMPORTANT: Worker's Services collection MUST be loaded here.
      // Consider using a specification like:
      // var spec = new WorkerByIdWithServicesSpec(request.WorkerId); 
      // var worker = await _workerRepository.FirstOrDefaultAsync(spec, cancellationToken);
      var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken); // TODO: Ensure Services are loaded!

      if (worker == null || worker.BusinessProfileId != request.BusinessProfileId)
      {
        _logger.LogWarning("Worker not found or does not belong to business. WorkerId: {WorkerId}, BusinessProfileId: {BusinessProfileId}", request.WorkerId, request.BusinessProfileId);
        return Result.NotFound($"Worker with ID {request.WorkerId} not found or does not belong to business {request.BusinessProfileId}.");
      }

      var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
      if (service == null || service.BusinessProfileId != request.BusinessProfileId)
      {
        _logger.LogWarning("Service to remove not found or does not belong to business. ServiceId: {ServiceId}, BusinessProfileId: {BusinessProfileId}", request.ServiceId, request.BusinessProfileId);
        return Result.NotFound($"Service with ID {request.ServiceId} not found or does not belong to business {request.BusinessProfileId}.");
      }

      // The Worker entity has a RemoveService method
      worker.RemoveService(service);

      await _workerRepository.UpdateAsync(worker, cancellationToken);
      // Ensure SaveChangesAsync is called if not handled by IRepository.UpdateAsync

      _logger.LogInformation("Service {ServiceId} removed from Worker {WorkerId}", request.ServiceId, request.WorkerId);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing service {ServiceId} from worker {WorkerId}", request.ServiceId, request.WorkerId);
      return Result.Error("An unexpected error occurred while removing the service.");
    }
  }
}
