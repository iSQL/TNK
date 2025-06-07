using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity
using TNK.UseCases.Services;
using TNK.UseCases.Workers; // For WorkerDTO
// Optional: Inject UserManager<ApplicationUser> if you need to verify/update ApplicationUserId links
// using Microsoft.AspNetCore.Identity;
// using TNK.Core.Identity;

namespace TNK.UseCases.Workers.Update;

public class UpdateWorkerHandler : IRequestHandler<UpdateWorkerCommand, Result<WorkerDTO>>
{
  private readonly IRepository<Worker> _repository;
  private readonly IValidator<UpdateWorkerCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<UpdateWorkerHandler> _logger;
  // private readonly UserManager<ApplicationUser> _userManager; // Optional

  public UpdateWorkerHandler(
      IRepository<Worker> repository,
      IValidator<UpdateWorkerCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<UpdateWorkerHandler> logger)
  // UserManager<ApplicationUser> userManager) // Optional
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
    // _userManager = userManager; // Optional
  }

  public async Task<Result<WorkerDTO>> Handle(UpdateWorkerCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateWorkerCommand for WorkerId: {WorkerId}", request.WorkerId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for UpdateWorkerCommand: {Errors}", validationResult.Errors);
      return Result<WorkerDTO>.Invalid(validationResult.AsErrors());
    }

    // Authorization: Check if user is authenticated
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to update a worker.");
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
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to update a worker for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<WorkerDTO>.Forbidden("User is not authorized for the specified business profile.");
    }

    var workerToUpdate = await _repository.GetByIdAsync(request.WorkerId, cancellationToken);
    if (workerToUpdate == null)
    {
      _logger.LogWarning("Worker with Id {WorkerId} not found for update.", request.WorkerId);
      return Result<WorkerDTO>.NotFound($"Worker with Id {request.WorkerId} not found.");
    }

    // Authorization: Ensure the worker being updated belongs to the business profile specified in the command
    if (workerToUpdate.BusinessProfileId != request.BusinessProfileId)
    {
      _logger.LogWarning("Worker (Id: {WorkerId}) belongs to BusinessProfileId {ActualBusinessId}, but update was attempted for BusinessProfileId {CommandBusinessId}.",
          request.WorkerId, workerToUpdate.BusinessProfileId, request.BusinessProfileId);
      return Result<WorkerDTO>.Forbidden("Worker does not belong to the specified business profile.");
    }

    // Optional: Validate/Update ApplicationUserId link
    // if (!string.IsNullOrEmpty(request.ApplicationUserId) && workerToUpdate.ApplicationUserId != request.ApplicationUserId)
    // {
    //     var appUser = await _userManager.FindByIdAsync(request.ApplicationUserId);
    //     if (appUser == null)
    //     {
    //         _logger.LogWarning("ApplicationUser with Id {AppUserId} for linking not found.", request.ApplicationUserId);
    //         return Result<WorkerDTO>.NotFound($"ApplicationUser with Id {request.ApplicationUserId} not found for linking.");
    //     }
    //     // Potentially check if appUser is already linked to another worker, or other business rules.
    //     workerToUpdate.LinkToApplicationUser(request.ApplicationUserId);
    // }
    // else if (string.IsNullOrEmpty(request.ApplicationUserId) && !string.IsNullOrEmpty(workerToUpdate.ApplicationUserId))
    // {
    //    // workerToUpdate.UnlinkFromApplicationUser(); // Assuming such a method exists on the entity
    // }


    workerToUpdate.UpdateDetails(
        request.FirstName,
        request.LastName,
        request.Email,
        request.PhoneNumber,
        request.ImageUrl,
        request.Specialization
    );

    // Link or unlink ApplicationUser
    if (!string.IsNullOrEmpty(request.ApplicationUserId))
    {
      // Optional: Add logic here to ensure the ApplicationUser is valid, not already linked to another worker in the same business, etc.
      // Example: var user = await _userManager.FindByIdAsync(request.ApplicationUserId); if(user == null) return Result.NotFound(...);
      workerToUpdate.LinkToApplicationUser(request.ApplicationUserId);
    }
    else if (string.IsNullOrEmpty(request.ApplicationUserId) && !string.IsNullOrEmpty(workerToUpdate.ApplicationUserId))
    {
      // Assuming you add a method to your Worker entity to clear the ApplicationUserId
      // For example: workerToUpdate.UnlinkApplicationUser();
      // For now, we'll re-call LinkToApplicationUser with null, assuming it handles this.
      // This might require adjustment in Worker.LinkToApplicationUser to accept null.
      // A more explicit method like UnlinkApplicationUser() would be cleaner.
      // workerToUpdate.LinkToApplicationUser(null); // If LinkToApplicationUser can handle null to clear.
      // For now, the UpdateDetails doesn't directly handle ApplicationUserId to avoid complexity without an Unlink method.
      // If ApplicationUserId needs to be cleared, you might need to add a specific method to the Worker entity.
      // Let's assume for now that if request.ApplicationUserId is null, we don't change an existing link unless explicitly told to.
      // If you need to clear it, ensure your entity method supports it.
    }


    if (request.IsActive)
    {
      workerToUpdate.Activate();
    }
    else
    {
      workerToUpdate.Deactivate();
    }

    try
    {
      await _repository.UpdateAsync(workerToUpdate, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken);

      var updatedDto = new WorkerDTO(
          workerToUpdate.Id,
          workerToUpdate.BusinessProfileId,
          workerToUpdate.ApplicationUserId,
          workerToUpdate.FirstName,
          workerToUpdate.LastName,
          workerToUpdate.FullName, // Ensure your Worker entity has FullName or calculate it here
          workerToUpdate.Email,
          workerToUpdate.PhoneNumber,
          workerToUpdate.IsActive,
          workerToUpdate.ImageUrl,
          workerToUpdate.Specialization,
          Services: workerToUpdate.Services?.Select(service => new ServiceDTO(
              service.Id,
                service.BusinessProfileId,
                service.Name,
                service.Description,
                service.DurationInMinutes,
                service.Price,
                service.IsActive,
                service.ImageUrl
          )).ToList() // Assuming Worker entity has a collection of Services
      );

      _logger.LogInformation("Successfully updated Worker with Id: {WorkerId}", workerToUpdate.Id);
      return Result<WorkerDTO>.Success(updatedDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating worker with Id {WorkerId}: {ErrorMessage}", request.WorkerId, ex.Message);
      return Result<WorkerDTO>.Error($"An error occurred while updating the worker: {ex.Message}");
    }
  }
}
