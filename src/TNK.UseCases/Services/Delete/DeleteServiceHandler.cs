using Ardalis.Result;
using Ardalis.Result.FluentValidation; // For AsErrors()
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.ServiceManagementAggregate.Entities;
// using TNK.Core.Interfaces; // Placeholder for your ICurrentUserService or similar

namespace TNK.UseCases.Services.Delete;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, Result>
{
  private readonly IRepository<Service> _repository;
  private readonly IValidator<DeleteServiceCommand> _validator;
  private readonly ILogger<DeleteServiceHandler> _logger;
  // private readonly ICurrentUserService _currentUserService; // For authorization

  public DeleteServiceHandler(
      IRepository<Service> repository,
      IValidator<DeleteServiceCommand> validator,
      ILogger<DeleteServiceHandler> logger)
  // ICurrentUserService currentUserService) // Uncomment and inject for authorization
  {
    _repository = repository;
    _validator = validator;
    _logger = logger;
    // _currentUserService = currentUserService; // Uncomment for authorization
  }

  public async Task<Result> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling DeleteServiceCommand for ServiceId: {ServiceId} and BusinessProfileId: {BusinessProfileId}", request.ServiceId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for DeleteServiceCommand: {Errors}", validationResult.Errors);
      return Result.Invalid(validationResult.AsErrors());
    }

    var serviceToDelete = await _repository.GetByIdAsync(request.ServiceId, cancellationToken);

    if (serviceToDelete == null)
    {
      _logger.LogWarning("Service with Id {ServiceId} not found for deletion.", request.ServiceId);
      // Consistent with NotFound result if the resource doesn't exist to be deleted.
      return Result.NotFound($"Service with Id {request.ServiceId} not found.");
    }

    // Authorization Check:
    // 1. TODO: Get the BusinessProfileId of the currently authenticated user (e.g., from _currentUserService.GetBusinessProfileIdAsync()).
    // 2. Compare it with request.BusinessProfileId to ensure the user is acting on behalf of the correct business.
    // 3. Compare serviceToDelete.BusinessProfileId with request.BusinessProfileId to ensure the service belongs to that business.

    // Example conceptual authorization:
    // var authenticatedUserBusinessProfileId = await _currentUserService.GetBusinessProfileIdAsync();
    // if (authenticatedUserBusinessProfileId == null || authenticatedUserBusinessProfileId != request.BusinessProfileId)
    // {
    //     _logger.LogWarning("User (Authenticated BusinessProfileId: {AuthUserBusinessId}) is not authorized or mismatch with command's BusinessProfileId ({CommandBusinessId}) for ServiceId {ServiceId}.",
    //         authenticatedUserBusinessProfileId, request.BusinessProfileId, request.ServiceId);
    //     return Result.Forbidden("User is not authorized for the specified business profile.");
    // }

    if (serviceToDelete.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Service's actual owner BusinessProfileId ({ServiceOwnerBusinessId}) does not match the command's BusinessProfileId ({CommandBusinessId}) for ServiceId {ServiceId}. Attempt to delete service not belonging to the specified business profile.",
          serviceToDelete.BusinessProfileId, request.BusinessProfileId, request.ServiceId);
      // This indicates the service does not belong to the business profile specified in the command.
      return Result.Forbidden("Operation not allowed. The service does not belong to the specified business profile.");
    }

    // Additional Business Logic (Optional):
    // Consider if there are conditions under which a service cannot be deleted.
    // For example, if it has active, upcoming bookings.
    // This might involve checking related entities (e.g., _bookingRepository.HasActiveBookingsForService(request.ServiceId))
    // if (await _someDomainService.ServiceHasActiveBookingsAsync(request.ServiceId, cancellationToken))
    // {
    //     _logger.LogWarning("Attempted to delete ServiceId {ServiceId} which has active bookings.", request.ServiceId);
    //     return Result.Error("Service cannot be deleted as it has active bookings. Please cancel or complete bookings first.");
    // }

    try
    {
      await _repository.DeleteAsync(serviceToDelete, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken); // Ensure the deletion is committed to the database.

      _logger.LogInformation("Successfully deleted Service with Id: {ServiceId}", serviceToDelete.Id);
      return Result.Success();
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) // More specific exception
    {
      _logger.LogError(dbEx, "Database error while deleting service with Id {ServiceId}: {ErrorMessage}", request.ServiceId, dbEx.Message);
      // This could be due to FK constraints if OnDelete is Restrict and related data still exists.
      return Result.Error($"A database error occurred while deleting the service. This might be due to existing related data (e.g., bookings). Details: {dbEx.InnerException?.Message ?? dbEx.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting service with Id {ServiceId}: {ErrorMessage}", request.ServiceId, ex.Message);
      return Result.Error($"An error occurred while deleting the service: {ex.Message}");
    }
  }
}
