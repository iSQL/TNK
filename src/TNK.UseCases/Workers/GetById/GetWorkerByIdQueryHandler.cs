using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity
using TNK.UseCases.Workers; // For WorkerDTO

namespace TNK.UseCases.Workers.GetById;

public class GetWorkerByIdQueryHandler : IRequestHandler<GetWorkerByIdQuery, Result<WorkerDTO>>
{
  private readonly IReadRepository<Worker> _repository;
  private readonly IValidator<GetWorkerByIdQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<GetWorkerByIdQueryHandler> _logger;

  public GetWorkerByIdQueryHandler(
      IReadRepository<Worker> repository,
      IValidator<GetWorkerByIdQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<GetWorkerByIdQueryHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<WorkerDTO>> Handle(GetWorkerByIdQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling GetWorkerByIdQuery for WorkerId: {WorkerId} and BusinessProfileId: {BusinessProfileId}", request.WorkerId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for GetWorkerByIdQuery: {Errors}", validationResult.Errors);
      return Result<WorkerDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization: Check if user is authenticated
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to get worker details.");
      return Result<WorkerDTO>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<WorkerDTO>.Forbidden("User is not associated with a business profile.");
    }

    // Authorization: Ensure the command's BusinessProfileId matches the authenticated user's BusinessProfileId (unless admin)
    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin"))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to query worker for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<WorkerDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var worker = await _repository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (worker == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found.", request.WorkerId);
      return Result<WorkerDTO>.NotFound($"Worker with Id {request.WorkerId} not found.");
    }

    // Authorization: Ensure the fetched worker actually belongs to the claimed BusinessProfileId
    if (worker.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker (Id: {WorkerId}) belongs to BusinessProfileId {ActualBusinessId}, but access was attempted for BusinessProfileId {QueryBusinessId}.",
          request.WorkerId, worker.BusinessProfileId, request.BusinessProfileId);
      return Result<WorkerDTO>.Forbidden("Access to this worker is not allowed for the specified business profile.");
    }

    var workerDto = new WorkerDTO(
        worker.Id,
        worker.BusinessProfileId,
        worker.ApplicationUserId,
        worker.FirstName,
        worker.LastName,
        worker.FullName, // Worker entity has FullName property
        worker.Email,
        worker.PhoneNumber,
        worker.IsActive,
        worker.ImageUrl,
        worker.Specialization
    );

    _logger.LogInformation("Successfully retrieved Worker with Id: {WorkerId}", worker.Id);
    return Result<WorkerDTO>.Success(workerDto);
  }
}
