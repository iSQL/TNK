// File: TNK.UseCases/Services/GetById/GetServiceByIdQueryHandler.cs
using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.UseCases.Services; // For ServiceDTO
using TNK.Core.Interfaces; 

namespace TNK.UseCases.Services.GetById;

public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, Result<ServiceDTO>>
{
  private readonly IReadRepository<Service> _repository; // Use IReadRepository for queries
  private readonly IValidator<GetServiceByIdQuery> _validator;
  private readonly ILogger<GetServiceByIdQueryHandler> _logger;
  private readonly ICurrentUserService _currentUserService; // For authorization

  public GetServiceByIdQueryHandler(
      IReadRepository<Service> repository,
      IValidator<GetServiceByIdQuery> validator,
      ILogger<GetServiceByIdQueryHandler> logger,
   ICurrentUserService currentUserService) // For authorization
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    _currentUserService = currentUserService;
  }

  public async Task<Result<ServiceDTO>> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling GetServiceByIdQuery for ServiceId: {ServiceId} and BusinessProfileId: {BusinessProfileId}", request.ServiceId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for GetServiceByIdQuery: {Errors}", validationResult.Errors);
      return Result<ServiceDTO>.Invalid(validationResult.AsErrors());
    }

    var service = await _repository.GetByIdAsync(request.ServiceId, cancellationToken);

    if (service == null)
    {
      _logger.LogWarning("Service with Id {ServiceId} not found.", request.ServiceId);
      return Result<ServiceDTO>.NotFound($"Service with Id {request.ServiceId} not found.");
    }

    // Authorization Check:
    // 1. TODO: Get the BusinessProfileId of the currently authenticated user.
    // 2. Ensure it matches request.BusinessProfileId.
    // 3. Ensure service.BusinessProfileId also matches request.BusinessProfileId.

    // Example conceptual authorization:
    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null || authenticatedUserBusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("User (Authenticated BusinessProfileId: {AuthUserBusinessId}) is not authorized or mismatch with command's BusinessProfileId ({CommandBusinessId}) for ServiceId {ServiceId}.",
          authenticatedUserBusinessProfileId, request.BusinessProfileId, request.ServiceId);
      return Result<ServiceDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    if (service.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Service's actual owner BusinessProfileId ({ServiceOwnerBusinessId}) does not match the query's BusinessProfileId ({QueryBusinessId}) for ServiceId {ServiceId}. User attempting to access service not belonging to their specified business profile.",
          service.BusinessProfileId, request.BusinessProfileId, request.ServiceId);
      // This indicates an attempt to access a service that does not belong to the business profile specified in the query,
      // or the service itself doesn't belong to the claimed business profile (which implies an issue or the service was found but shouldn't be accessible to this businessId).
      return Result<ServiceDTO>.Forbidden("Access to this service is not allowed for the specified business profile.");
    }

    var serviceDto = new ServiceDTO(
        service.Id,
        service.BusinessProfileId,
        service.Name,
        service.Description,
        service.DurationInMinutes,
        service.Price,
        service.IsActive,
        service.ImageUrl
    );

    _logger.LogInformation("Successfully retrieved Service with Id: {ServiceId}", service.Id);
    return Result<ServiceDTO>.Success(serviceDto);
  }
}
