using System.Security.Claims; // Required for ClaimTypes
using Ardalis.Result;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity; // Required for UserManager (though less used now for this specific check)
using TNK.Core.Constants; // Assuming your Roles constants are here
using TNK.Core.Identity; // For ApplicationUser
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Services; // For ServiceDTO
using TNK.UseCases.Services.ListByBusiness;
// Ensure correct using for ListServicesByBusinessRequest and ListServicesByBusinessResponse
using TNK.Web.Services.ListByBusiness;

namespace TNK.Web.Services.ListByBusiness;

/// <summary>
/// List all Services for a specific Business Profile.
/// </summary>
/// <remarks>
/// Retrieves a list of services associated with the given BusinessProfileId.
/// Accessible by Administrators or the owner of the Business Profile.
/// </remarks>
public class ListServicesByBusiness : Endpoint<ListServicesByBusinessRequest, ListServicesByBusinessResponse>
{
  private readonly IMediator _mediator;
  // UserManager might still be needed if you require more ApplicationUser details for other reasons not apparent here.
  // private readonly UserManager<ApplicationUser> _userManager; 
  private readonly ICurrentUserService _currentUserService;

  public ListServicesByBusiness(
    IMediator mediator,
    // UserManager<ApplicationUser> userManager, 
    ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    // _userManager = userManager;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Get(ListServicesByBusinessRequest.Route);
    Description(d => d.AutoTagOverride("Services"));
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Summary(s =>
    {
      s.Summary = "List services for a business.";
      s.Description = "Retrieves a list of services associated with the given business profile ID. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[200] = "A list of services for the business profile.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to access this business profile's services.";
      s.Responses[404] = "Business profile not found.";
    });
  }

  public override async Task HandleAsync(ListServicesByBusinessRequest request, CancellationToken cancellationToken)
  {
    // Check if the user's identity is not null and if the user is authenticated.
    // User property in FastEndpoints is a ClaimsPrincipal.
    // IsAuthenticated is a property of IIdentity, accessed via User.Identity.
    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    // Authorization check
    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);

    if (!isAuthorized)
    {
      // Check if the current user (from claims/ICurrentUserService) owns the requested BusinessProfileId
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;

      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != request.BusinessProfileId)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to access BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    // The query to fetch services
    var query = new ListServicesByBusinessQuery(request.BusinessProfileId);
    Result<List<ServiceDTO>> result = await _mediator.Send(query, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        Response = new ListServicesByBusinessResponse
        {
          Services = result.Value
        };
        await SendAsync(Response, StatusCodes.Status200OK, cancellationToken);
        break;
      case ResultStatus.Invalid:
        AddError(r => r.BusinessProfileId, string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage)));
        await SendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        break;
      case ResultStatus.NotFound:
        await SendNotFoundAsync(cancellationToken);
        break;
      case ResultStatus.Unauthorized:
        await SendUnauthorizedAsync(cancellationToken);
        break;
      case ResultStatus.Forbidden: // Could be returned by a deeper layer if additional checks fail
        await SendForbiddenAsync(cancellationToken);
        break;
      case ResultStatus.Error:
        Logger.LogError("An error occurred while listing services for BusinessProfileId {BusinessProfileId}: {Errors}", request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for BusinessProfileId {BusinessProfileId}", result.Status, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
