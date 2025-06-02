using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity
using TNK.UseCases.Workers.Specifications; // For WorkersByBusinessSpec
using TNK.UseCases.Workers; // For WorkerDTO
// using TNK.UseCases.Common.Models; // For PagedResult if using pagination

namespace TNK.UseCases.Workers.ListByBusiness;

public class ListWorkersByBusinessQueryHandler : IRequestHandler<ListWorkersByBusinessQuery, Result<List<WorkerDTO>>>
{
  private readonly IReadRepository<Worker> _repository;
  private readonly IValidator<ListWorkersByBusinessQuery> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<ListWorkersByBusinessQueryHandler> _logger;

  public ListWorkersByBusinessQueryHandler(
      IReadRepository<Worker> repository,
      IValidator<ListWorkersByBusinessQuery> validator,
      ICurrentUserService currentUserService,
      ILogger<ListWorkersByBusinessQueryHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result<List<WorkerDTO>>> Handle(ListWorkersByBusinessQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ListWorkersByBusinessQuery for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ListWorkersByBusinessQuery: {Errors}", validationResult.Errors);
      return Result<List<WorkerDTO>>.Invalid(validationResult.AsErrors());
    }

    // Authorization: Check if user is authenticated
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to list workers.");
      return Result<List<WorkerDTO>>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<List<WorkerDTO>>.Forbidden("User is not associated with a business profile.");
    }

    // Authorization: Ensure the command's BusinessProfileId matches the authenticated user's BusinessProfileId (unless admin)
    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin"))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to list workers for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<List<WorkerDTO>>.Forbidden("User is not authorized for the specified business profile.");
    }

    var spec = new WorkersByBusinessSpec(request.BusinessProfileId);
    // If implementing pagination:
    // var spec = new WorkersByBusinessSpec(request.BusinessProfileId, (request.PageNumber - 1) * request.PageSize, request.PageSize);
    // var totalRecords = await _repository.CountAsync(new WorkersByBusinessSpec(request.BusinessProfileId), cancellationToken);

    var workers = await _repository.ListAsync(spec, cancellationToken);

    if (workers == null) // Should typically be an empty list if no workers are found, not null
    {
      _logger.LogWarning("Worker list returned null for BusinessProfileId {BusinessProfileId}", request.BusinessProfileId);
      return Result<List<WorkerDTO>>.Success(new List<WorkerDTO>()); // Return empty list
    }

    var workerDtos = workers.Select(worker => new WorkerDTO(
            worker.Id,
            worker.BusinessProfileId,
            worker.ApplicationUserId,
            worker.FirstName,
            worker.LastName,
            worker.FullName, // Worker entity has this property
            worker.Email,
            worker.PhoneNumber,
            worker.IsActive,
            worker.ImageUrl,
            worker.Specialization))
        .ToList();

    // If implementing pagination:
    // var pagedResult = new PagedResult<WorkerDTO>(workerDtos, totalRecords, request.PageNumber, request.PageSize);
    // _logger.LogInformation("Successfully retrieved {Count} workers for BusinessProfileId: {BusinessProfileId}, Page: {PageNumber}", workerDtos.Count, request.BusinessProfileId, request.PageNumber);
    // return Result<PagedResult<WorkerDTO>>.Success(pagedResult);

    _logger.LogInformation("Successfully retrieved {Count} workers for BusinessProfileId: {BusinessProfileId}", workerDtos.Count, request.BusinessProfileId);
    return Result<List<WorkerDTO>>.Success(workerDtos);
  }
}
