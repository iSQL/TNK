using Ardalis.Result.FluentValidation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
// Assuming you have a way to get current user's BusinessProfileId if needed
// using TNK.Core.Interfaces; // For something like ICurrentUserService

namespace TNK.UseCases.Services.Create;

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, Result<Guid>>
{
  private readonly IRepository<Service> _repository; // Using generic IRepository<Service>
  private readonly IValidator<CreateServiceCommand> _validator;
  private readonly ILogger<CreateServiceHandler> _logger;
  // private readonly ICurrentUserService _currentUserService; // Example if needed for auth/scoping

  public CreateServiceHandler(
      IRepository<Service> repository,
      IValidator<CreateServiceCommand> validator,
      ILogger<CreateServiceHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    // _currentUserService = currentUserService;
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

    // Authorization check: Ensure the current user is authorized to create a service for the given BusinessProfileId.
    // This logic might involve an ICurrentUserService or similar to get the authenticated user's claims
    // and verify they own/manage the request.BusinessProfileId.
    // For example:
    // var currentUserBusinessProfileId = await _currentUserService.GetBusinessProfileIdAsync();
    // if (currentUserBusinessProfileId != request.BusinessProfileId && !await _currentUserService.IsAdminAsync())
    // {
    //     _logger.LogWarning("User not authorized to create service for BusinessProfileId {TargetBusinessProfileId}", request.BusinessProfileId);
    //     return Result<Guid>.Unauthorized();
    // }


    var newService = new Service(
        request.BusinessProfileId,
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
    // IsActive is true by default in the Service constructor.

    try
    {
      var createdService = await _repository.AddAsync(newService, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken); // Ensure changes are saved if AddAsync doesn't do it immediately.

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
      // It's often better to return a generic error to the client unless specific errors are expected and handled.
      return Result<Guid>.Error($"An error occurred while creating the service: {ex.Message}");
    }
  }
}
