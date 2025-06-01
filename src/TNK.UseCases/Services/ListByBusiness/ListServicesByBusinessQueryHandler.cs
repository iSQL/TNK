// File: TNK.UseCases/Services/ListByBusiness/ListServicesByBusinessQueryHandler.cs
using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel; // For IReadRepository
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.UseCases.Services.Specifications; // For ServicesByBusinessSpec
using TNK.UseCases.Services; // For ServiceDTO
// using TNK.Core.Interfaces; // For ICurrentUserService
// using TNK.UseCases.Common.Models; // For PagedResult if using pagination

namespace TNK.UseCases.Services.ListByBusiness;

public class ListServicesByBusinessQueryHandler : IRequestHandler<ListServicesByBusinessQuery, Result<List<ServiceDTO>>>
{
  private readonly IReadRepository<Service> _repository;
  private readonly IValidator<ListServicesByBusinessQuery> _validator;
  private readonly ILogger<ListServicesByBusinessQueryHandler> _logger;
  // private readonly ICurrentUserService _currentUserService; // For authorization

  public ListServicesByBusinessQueryHandler(
      IReadRepository<Service> repository,
      IValidator<ListServicesByBusinessQuery> validator,
      ILogger<ListServicesByBusinessQueryHandler> logger)
  // ICurrentUserService currentUserService) // For authorization
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    // _currentUserService = currentUserService;
  }

  public async Task<Result<List<ServiceDTO>>> Handle(ListServicesByBusinessQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling ListServicesByBusinessQuery for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for ListServicesByBusinessQuery: {Errors}", validationResult.Errors);
      return Result<List<ServiceDTO>>.Invalid(validationResult.AsErrors());
    }

    // Authorization Check:
    // TODO: Ensure the currently authenticated user is authorized to view services for request.BusinessProfileId.
    // This usually means their own BusinessProfileId should match request.BusinessProfileId, or they are an admin.
    // Example:
    // var authenticatedUserBusinessProfileId = await _currentUserService.GetBusinessProfileIdAsync();
    // if (authenticatedUserBusinessProfileId == null || (authenticatedUserBusinessProfileId != request.BusinessProfileId && !await _currentUserService.IsAdminAsync()))
    // {
    //     _logger.LogWarning("User (Authenticated BusinessProfileId: {AuthUserBusinessId}) is not authorized to list services for BusinessProfileId: {QueryBusinessId}.",
    //         authenticatedUserBusinessProfileId, request.BusinessProfileId);
    //     return Result<List<ServiceDTO>>.Forbidden("User is not authorized for the specified business profile.");
    // }

    var spec = new ServicesByBusinessSpec(request.BusinessProfileId);
    // If implementing pagination:
    // var spec = new ServicesByBusinessSpec(request.BusinessProfileId, (request.PageNumber - 1) * request.PageSize, request.PageSize);
    // var totalRecords = await _repository.CountAsync(new ServicesByBusinessSpec(request.BusinessProfileId), cancellationToken); // For PagedResult

    var services = await _repository.ListAsync(spec, cancellationToken);

    if (services == null) // ListAsync usually returns an empty list, not null, but defensive check.
    {
      _logger.LogWarning("Received null list of services for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);
      return Result<List<ServiceDTO>>.Success(new List<ServiceDTO>()); // Return empty list
    }

    var serviceDtos = services.Select(service => new ServiceDTO(
            service.Id,
            service.BusinessProfileId,
            service.Name,
            service.Description,
            service.DurationInMinutes,
            service.Price,
            service.IsActive,
            service.ImageUrl))
        .ToList();

    // If implementing pagination:
    // var pagedResult = new PagedResult<ServiceDTO>(serviceDtos, totalRecords, request.PageNumber, request.PageSize);
    // _logger.LogInformation("Successfully retrieved {Count} services for BusinessProfileId: {BusinessProfileId}, Page: {PageNumber}", serviceDtos.Count, request.BusinessProfileId, request.PageNumber);
    // return Result<PagedResult<ServiceDTO>>.Success(pagedResult);

    _logger.LogInformation("Successfully retrieved {Count} services for BusinessProfileId: {BusinessProfileId}", serviceDtos.Count, request.BusinessProfileId);
    return Result<List<ServiceDTO>>.Success(serviceDtos);
  }
}
