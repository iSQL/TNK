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

namespace TNK.UseCases.Workers.AssignService;

public class AssignServiceToWorkerCommandHandler : IRequestHandler<AssignServiceToWorkerCommand, Result>
{
  private readonly IRepository<Worker> _workerRepository;
  private readonly IReadRepository<Service> _serviceRepository;
  private readonly ILogger<AssignServiceToWorkerCommandHandler> _logger;

  public AssignServiceToWorkerCommandHandler(
      IRepository<Worker> workerRepository,
      IReadRepository<Service> serviceRepository,
      ILogger<AssignServiceToWorkerCommandHandler> logger)
  {
    _workerRepository = workerRepository;
    _serviceRepository = serviceRepository;
    _logger = logger;
  }

  public async Task<Result> Handle(AssignServiceToWorkerCommand request, CancellationToken cancellationToken)
  {
    try
    {
      // It's crucial that the Worker entity is loaded with its Services collection.
      // This might require a custom specification or an Include in the repository method.
      // For Ardalis.Specification, a spec like WorkerByIdWithServicesSpec would be used.
      var worker = await _workerRepository.GetByIdAsync(request.WorkerId, cancellationToken); // TODO: Ensure Services are loaded
      if (worker == null || worker.BusinessProfileId != request.BusinessProfileId)
      {
        _logger.LogWarning("Worker not found or does not belong to business. WorkerId: {WorkerId}, BusinessProfileId: {BusinessProfileId}", request.WorkerId, request.BusinessProfileId);
        return Result.NotFound($"Worker with ID {request.WorkerId} not found or does not belong to business {request.BusinessProfileId}.");
      }

      var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
      if (service == null || service.BusinessProfileId != request.BusinessProfileId)
      {
        _logger.LogWarning("Service not found or does not belong to business. ServiceId: {ServiceId}, BusinessProfileId: {BusinessProfileId}", request.ServiceId, request.BusinessProfileId);
        return Result.NotFound($"Service with ID {request.ServiceId} not found or does not belong to business {request.BusinessProfileId}.");
      }

      // The Worker entity now has an AddService method
      worker.AddService(service);

      await _workerRepository.UpdateAsync(worker, cancellationToken);
      // Ardalis.Specification repositories typically require SaveChangesAsync to be called on the DbContext explicitly
      // or via a UnitOfWork. If your IRepository<Worker> handles SaveChanges implicitly, this is fine.
      // Otherwise, ensure SaveChangesAsync() is called.

      _logger.LogInformation("Service {ServiceId} assigned to Worker {WorkerId}", request.ServiceId, request.WorkerId);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error assigning service {ServiceId} to worker {WorkerId}", request.ServiceId, request.WorkerId);
      return Result.Error("An unexpected error occurred while assigning the service.");
    }
  }
}
