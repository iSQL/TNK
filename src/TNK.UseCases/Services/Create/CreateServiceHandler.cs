using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.Core.Interfaces; // For ICurrentUserService

namespace TNK.UseCases.Services.Create;

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, Result<Guid>>
{
  private readonly IRepository<Service> _repository;
  private readonly IValidator<CreateServiceCommand> _validator;
  private readonly ILogger<CreateServiceHandler> _logger;
  private readonly ICurrentUserService _currentUserService; // Inject the service

  public CreateServiceHandler(
      IRepository<Service> repository,
      IValidator<CreateServiceCommand> validator,
      ILogger<CreateServiceHandler> logger,
      ICurrentUserService currentUserService) // Add to constructor
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    _currentUserService = currentUserService; // Assign
  }

  public async Task<Result<Guid>> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling CreateServiceCommand for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for CreateServiceCommand: {Errors}", validationResult.Errors);
      return Result<Guid>.Invalid(validationResult.AsErrors());
    }

    // Authorization check:
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated.");
      return Result<Guid>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;

    // Ensure the user is associated with a business profile
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("User {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<Guid>.Forbidden("User is not associated with a business profile.");
    }

    // Ensure the command's BusinessProfileId matches the authenticated user's BusinessProfileId
    // This is crucial for vendor owners creating services for their own business.
    // Admins might have different logic.
    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")) // Example admin override
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to create a service for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<Guid>.Forbidden("User is not authorized for the specified business profile.");
    }

    var newService = new Service(
        request.BusinessProfileId, // Use the validated and authorized BusinessProfileId
        request.Name,
        request.DurationInMinutes,
        request.Price
    );

    if (request.Description != null)
    {
      newService.SetDescription(request.Description);
    }

    if (request.ImageUrl != null)
    {
      newService.SetImageUrl(request.ImageUrl);
    }

    try
    {
      var createdService = await _repository.AddAsync(newService, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      if (createdService == null || createdService.Id == Guid.Empty)
      {
        _logger.LogError("Failed to create service for BusinessProfileId: {BusinessProfileId} - Repository returned null or empty Id.", request.BusinessProfileId);
        return Result<Guid>.Error("Failed to create service due to a repository issue.");
      }

      _logger.LogInformation("Successfully created Service with Id: {ServiceId} for BusinessProfileId: {BusinessProfileId}", createdService.Id, request.BusinessProfileId);
      return Result<Guid>.Success(createdService.Id);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating service for BusinessProfileId {BusinessProfileId}: {ErrorMessage}", request.BusinessProfileId, ex.Message);
      return Result<Guid>.Error($"An error occurred while creating the service: {ex.Message}");
    }
  }
}
