using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
using TNK.UseCases.Services; // For ServiceDto
using TNK.Core.ServiceManagementAggregate.Interfaces; // Specific repository if preferred
using TNK.Core.Interfaces; // For ICurrentUserService

namespace TNK.UseCases.Services.Update;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, Result<ServiceDTO>>
{
  private readonly IRepository<Service> _repository;
  private readonly IValidator<UpdateServiceCommand> _validator;
  private readonly ILogger<UpdateServiceHandler> _logger;
  private readonly ICurrentUserService _currentUserService; 

  public UpdateServiceHandler(
      IRepository<Service> repository,
      IValidator<UpdateServiceCommand> validator,
      ILogger<UpdateServiceHandler> logger,
   ICurrentUserService currentUserService) 
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    _currentUserService = currentUserService;
  }

  public async Task<Result<ServiceDTO>> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateServiceCommand for ServiceId: {ServiceId}", request.ServiceId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateServiceCommand: {Errors}", validationResult.Errors);
      return Result<ServiceDTO>.Invalid(validationResult.AsErrors());
    }

    var serviceToUpdate = await _repository.GetByIdAsync(request.ServiceId, cancellationToken);
    if (serviceToUpdate == null)
    {
      _logger.LogWarning("Service with Id {ServiceId} not found for update.", request.ServiceId);
      return Result<ServiceDTO>.NotFound($"Service with Id {request.ServiceId} not found.");
    }


     var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("User (BusinessProfileId: {UserBusinessProfileId}) attempting to update service using an incorrect command BusinessProfileId ({CommandBusinessProfileId}) for ServiceId {ServiceId}.",
          authenticatedUserBusinessProfileId, request.BusinessProfileId, request.ServiceId);
      return Result<ServiceDTO>.Forbidden("Operation not allowed.");
    }

    if (serviceToUpdate.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Service's actual BusinessProfileId ({ServiceOwnerBusinessProfileId}) does not match the command's BusinessProfileId ({CommandBusinessProfileId}) for ServiceId {ServiceId}. Potential tampering or incorrect data.",
         serviceToUpdate.BusinessProfileId, request.BusinessProfileId, request.ServiceId);
      // This implies the service record itself does not belong to the business profile ID provided in the command.
      return Result<ServiceDTO>.Forbidden("Service does not belong to the specified business profile.");
    }


    serviceToUpdate.UpdateDetails(
        request.Name,
        request.Description,
        request.DurationInMinutes,
        request.Price,
        request.ImageUrl
    );

    if (request.IsActive)
    {
      serviceToUpdate.Activate();
    }
    else
    {
      serviceToUpdate.Deactivate();
    }

    try
    {
      await _repository.UpdateAsync(serviceToUpdate, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken); // Ensure changes are saved

      var updatedDTO = new ServiceDTO(
          serviceToUpdate.Id,
          serviceToUpdate.BusinessProfileId,
          serviceToUpdate.Name,
          serviceToUpdate.Description,
          serviceToUpdate.DurationInMinutes,
          serviceToUpdate.Price,
          serviceToUpdate.IsActive,
          serviceToUpdate.ImageUrl
      );

      _logger.LogInformation("Successfully updated Service with Id: {ServiceId}", serviceToUpdate.Id);
      return Result<ServiceDTO>.Success(updatedDTO);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating service with Id {ServiceId}: {ErrorMessage}", request.ServiceId, ex.Message);
      return Result<ServiceDTO>.Error($"An error occurred while updating the service: {ex.Message}");
    }
  }
}
