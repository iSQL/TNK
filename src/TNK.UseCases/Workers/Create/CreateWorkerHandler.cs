using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using Ardalis.SharedKernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity
// Optional: Inject UserManager<ApplicationUser> if you need to verify ApplicationUserId exists
// using Microsoft.AspNetCore.Identity;
// using TNK.Core.Identity;


namespace TNK.UseCases.Workers.Create;

public class CreateWorkerHandler : IRequestHandler<CreateWorkerCommand, Result<Guid>>
{
  private readonly IRepository<Worker> _repository;
  private readonly IValidator<CreateWorkerCommand> _validator;
  private readonly ICurrentUserService _currentUserService;
  private readonly ILogger<CreateWorkerHandler> _logger;
  // private readonly UserManager<ApplicationUser> _userManager; // Optional

  public CreateWorkerHandler(
      IRepository<Worker> repository,
      IValidator<CreateWorkerCommand> validator,
      ICurrentUserService currentUserService,
      ILogger<CreateWorkerHandler> logger)
  // UserManager<ApplicationUser> userManager) // Optional constructor parameter
  {
    _repository = repository;
    _validator = validator;
    _currentUserService = currentUserService;
    _logger = logger;
    // _userManager = userManager; // Optional assignment
  }

  public async Task<Result<Guid>> Handle(CreateWorkerCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling CreateWorkerCommand for BusinessProfileId: {BusinessProfileId}", request.BusinessProfileId);

    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogWarning("Validation failed for CreateWorkerCommand: {Errors}", validationResult.Errors);
      return Result<Guid>.Invalid(validationResult.AsErrors());
    }

    // Authorization
    if (!_currentUserService.IsAuthenticated)
    {
      _logger.LogWarning("User is not authenticated to create a worker.");
      return Result<Guid>.Unauthorized();
    }

    var authenticatedUserBusinessProfileId = _currentUserService.BusinessProfileId;
    if (authenticatedUserBusinessProfileId == null)
    {
      _logger.LogWarning("Authenticated user {UserId} is not associated with any BusinessProfileId.", _currentUserService.UserId);
      return Result<Guid>.Forbidden("User is not associated with a business profile.");
    }

    if (authenticatedUserBusinessProfileId != request.BusinessProfileId && !_currentUserService.IsInRole("Admin")) // Admin check example
    {
      _logger.LogWarning("User (BusinessProfileId: {AuthUserBusinessId}) is not authorized to create a worker for the target BusinessProfileId ({TargetBusinessId}).",
          authenticatedUserBusinessProfileId, request.BusinessProfileId);
      return Result<Guid>.Forbidden("User is not authorized for the specified business profile.");
    }

    // Optional: Deeper validation for ApplicationUserId if provided
    // if (!string.IsNullOrEmpty(request.ApplicationUserId))
    // {
    //     var appUser = await _userManager.FindByIdAsync(request.ApplicationUserId);
    //     if (appUser == null)
    //     {
    //         _logger.LogWarning("ApplicationUser with Id {AppUserId} not found when creating worker.", request.ApplicationUserId);
    //         return Result<Guid>.NotFound($"ApplicationUser with Id {request.ApplicationUserId} not found.");
    //     }
    //     // Further checks: Is appUser.BusinessProfileId null or does it match request.BusinessProfileId?
    //     // Is this appUser already linked to another worker in this business?
    // }

    var newWorker = new Worker(
        request.BusinessProfileId,
        request.FirstName,
        request.LastName
    );

    // Use the entity's method to set other details
    newWorker.UpdateDetails(
        request.FirstName, // Pass again if UpdateDetails is comprehensive
        request.LastName,  // Pass again
        request.Email,
        request.PhoneNumber,
        request.ImageUrl,
        request.Specialization
    );

    if (!string.IsNullOrEmpty(request.ApplicationUserId))
    {
      newWorker.LinkToApplicationUser(request.ApplicationUserId);
    }
    // IsActive is true by default from Worker constructor

    try
    {
      var createdWorker = await _repository.AddAsync(newWorker, cancellationToken);
      await _repository.SaveChangesAsync(cancellationToken); // Ensure changes are saved

      _logger.LogInformation("Successfully created Worker with Id: {WorkerId} for BusinessProfileId: {BusinessProfileId}", createdWorker.Id, request.BusinessProfileId);
      return Result<Guid>.Success(createdWorker.Id);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating worker for BusinessProfileId {BusinessProfileId}: {ErrorMessage}", request.BusinessProfileId, ex.Message);
      return Result<Guid>.Error($"An error occurred while creating the worker: {ex.Message}");
    }
  }
}
