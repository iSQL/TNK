using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging; 
using TNK.Core.Interfaces;       // For IBusinessProfileRepository
using System.Threading;
using System.Threading.Tasks;

namespace TNK.UseCases.BusinessProfiles.DeleteAdmin;

/// <summary>
/// Handles the deletion of a specific Business Profile by its ID, initiated by a SuperAdmin.
/// </summary>
public class AdminDeleteBusinessProfileCommandHandler : IRequestHandler<AdminDeleteBusinessProfileCommand, Result>
{
  private readonly IBusinessProfileRepository _businessProfileRepository;
  private readonly ILogger<AdminDeleteBusinessProfileCommandHandler> _logger; // Optional

  public AdminDeleteBusinessProfileCommandHandler(
      IBusinessProfileRepository businessProfileRepository,
      ILogger<AdminDeleteBusinessProfileCommandHandler> logger) // Optional: ILogger
  {
    _businessProfileRepository = businessProfileRepository;
    _logger = logger; // Optional
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
      // Note: SaveChangesAsync is typically called by the DeleteAsync method in EfRepository or by a Unit of Work pattern.
      // If your IRepository/EfRepository setup requires explicit SaveChangesAsync call after DeleteAsync, you might need to manage that,
      // possibly through a IUnitOfWork interface or ensuring your repository handles it.
      // For Ardalis templates, EfRepository.DeleteAsync usually calls SaveChangesAsync.

      _logger.LogInformation("Business Profile with ID {BusinessProfileId} was deleted successfully by a SuperAdmin.", request.BusinessProfileId);
      return Result.Success(); // Or Result.NoContent() if preferred for DELETE operations
    }
    catch (System.Exception ex)
    {
      // Log the exception details
      _logger.LogError(ex, "Error occurred while deleting Business Profile with ID {BusinessProfileId}.", request.BusinessProfileId);
      // It's generally better to let a global exception handler manage the exact HTTP response for unexpected errors.
      // However, you can return a specific error result if needed.
      return Result.Error($"An error occurred while deleting the Business Profile: {ex.Message}");
    }
  }
}
