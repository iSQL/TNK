using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore; // For DbUpdateException
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity

namespace TNK.UseCases.Workers.Delete;

public class DeleteWorkerHandler : IRequestHandler<DeleteWorkerCommand, Result>
{
  private readonly IRepository<Worker> _repository;
  private readonly IValidator<DeleteWorkerCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<DeleteWorkerHandler> _logger;

  public DeleteWorkerHandler(
      IRepository<Worker> repository,
      IValidator<DeleteWorkerCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<DeleteWorkerHandler> logger)
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
  }

  public async Task<Result> Handle(DeleteWorkerCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling DeleteWorkerCommand for WorkerId: {WorkerId} and BusinessProfileId: {BusinessProfileId}", request.WorkerId, request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for DeleteWorkerCommand: {Errors}", validationResult.Errors);
      return Result.Invalid(validationResult.AsErrors());
    }

    // Authorization: Check if user is authenticated
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to delete a worker.");
      return Result.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result.Forbidden("User is not associated with a business profile.");
    }

    // Authorization: Ensure the command's BusinessProfileId matches the authenticated user's BusinessProfileId (unless admin)
    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin"))
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to delete a worker for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result.Forbidden("User is not authorized for the specified business profile.");
    }

    var workerToDelete = await _repository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (workerToDelete == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found for deletion.", request.WorkerId);
      return Result.NotFound($"Worker with Id {request.WorkerId} not found.");
    }

    // Authorization: Ensure the worker being deleted belongs to the business profile specified in the command
    if (workerToDelete.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker (Id: {WorkerId}) belongs to BusinessProfileId {ActualBusinessId}, but deletion was attempted for BusinessProfileId {CommandBusinessId}.",
          request.WorkerId, workerToDelete.BusinessProfileId, request.BusinessProfileId);
      return Result.Forbidden("Worker does not belong to the specified business profile.");
    }

    // Business Logic Consideration:
    // Deleting a worker might be problematic if they have upcoming bookings.
    // Your BookingConfiguration has OnDelete(DeleteBehavior.Restrict) for the WorkerId foreign key.
    // This means EF Core will prevent deletion at the DB level if bookings exist, throwing a DbUpdateException.
    // You could proactively check for active/upcoming bookings here if you want to provide a more specific error message
    // before attempting the delete, e.g., by injecting IBookingRepository.
    // For example:
    // if (await _bookingRepository.WorkerHasActiveBookingsAsync(request.WorkerId, cancellationToken))
    // {
    //     _logger.LogWarning("Attempt to delete worker {WorkerId} who has active bookings.", request.WorkerId);
    //     return Result.Error("Worker cannot be deleted as they have active or upcoming bookings. Please reassign or cancel these bookings first.");
    // }
    // Note: Schedules and AvailabilitySlots are configured with Cascade delete, so they will be removed.

    try
    {
      await _repository.DeleteAsync(workerToDelete, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully deleted Worker with Id: {WorkerId}", workerToDelete.Id);
      return Result.Success();
    }
    catch (DbUpdateException dbEx)
    {
      _logger.LogError(dbEx, "Database error while deleting worker with Id {WorkerId}. This may be due to existing bookings linked to this worker.", request.WorkerId);
      return Result.Error($"A database error occurred: Worker cannot be deleted, likely due to existing bookings. Please ensure all bookings for this worker are resolved. Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting worker with Id {WorkerId}: {ErrorMessage}", request.WorkerId, ex.Message);
      return Result.Error($"An error occurred while deleting the worker: {ex.Message}");
    }
  }
}
