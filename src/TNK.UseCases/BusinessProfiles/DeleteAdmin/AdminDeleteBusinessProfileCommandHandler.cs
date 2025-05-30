using MediatR;
using Microsoft.Extensions.Logging;
using TNK.Core.Interfaces;       

namespace TNK.UseCases.BusinessProfiles.DeleteAdmin;

/// <summary>
/// Handles the deletion of a specific Business Profile by its ID, initiated by a SuperAdmin.
/// </summary>
public class AdminDeleteBusinessProfileCommandHandler : IRequestHandler<AdminDeleteBusinessProfileCommand, Result>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly ILogger<AdminDeleteBusinessProfileCommandHandler> _logger; 

  public AdminDeleteBusinessProfileCommandHandler(
      IBusinessProfileRepository businessProfileRepository,
      ILogger<AdminDeleteBusinessProfileCommandHandler> logger) 
  {
    _businessProfileRepository = businessProfileRepository;
    _logger = logger; 
  }

  public async Task<Result> Handle(AdminDeleteBusinessProfileCommand request, CancellationToken cancellationToken)
  {
    var businessProfileToDelete = await _businessProfileRepository.GetByIdAsync(request.BusinessProfileId, cancellationToken);

    if (businessProfileToDelete == null)
    {
      _logger.LogWarning("Attempted to delete non-existent Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.NotFound($"No Business Profile found with ID {request.BusinessProfileId} to delete.");
    }

    try
    {
      await _businessProfileRepository.DeleteAsync(businessProfileToDelete, cancellationToken);


      _logger.LogInformation("Business Profile with ID {BusinessProfileId} was deleted successfully by a SuperAdmin.", request.BusinessProfileId);
      return Result.Success(); 
    }
    catch (System.Exception ex)
    {
      _logger.LogError(ex, "Error occurred while deleting Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      return Result.Error($"An error occurred while deleting the Business Profile: {ex.Message}");
    }
  }
}
