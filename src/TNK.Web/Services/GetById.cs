using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants;
using TNK.Core.Interfaces;
using TNK.UseCases.Services; // For ServiceDTO
using TNK.UseCases.Services.GetById; // For GetServiceByIdQuery

namespace TNK.Web.Services.GetById;

// Request DTO: Parameters will be bound from the route
public class GetServiceByIdRequest
{
  // These will be bound from the route: /Businesses/{BusinessProfileId}/Services/{ServiceId}
  // No need for explicit properties if FastEndpoints binds them directly to the handler's Route<T> methods.
  // However, defining them can be useful for clarity and for the validator.
  public int BusinessProfileId { get; set; }
  public Guid ServiceId { get; set; }
}

// Validator for route parameters (though FastEndpoints handles route constraints,
// an explicit validator can add more specific messages or logic if needed)
public class GetServiceByIdRequestValidator : AbstractValidator<GetServiceByIdRequest>
{
  public GetServiceByIdRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.ServiceId)
      .NotEmpty().WithMessage("ServiceId is required.");
  }
}

/// <summary>
/// Get a specific Service by its ID, scoped to a Business Profile.
/// </summary>
/// <remarks>
/// Retrieves details for a specific service.
/// Accessible by Administrators or the owner of the Business Profile.
/// </remarks>
public class GetServiceByIdEndpoint : Endpoint<GetServiceByIdRequest, ServiceDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public GetServiceByIdEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Get("/Businesses/{BusinessProfileId}/Services/{ServiceId:guid}");
    Description(d => d.AutoTagOverride("Services"));
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<GetServiceByIdRequestValidator>(); // Explicitly register the nested validator

    Summary(s =>
    {
      s.Summary = "Get a service by ID.";
      s.Description = "Retrieves a specific service by its ID, scoped to a business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[200] = "The requested service details.";
      s.Responses[400] = "Invalid request parameters (e.g., invalid ID format).";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to access this service.";
      s.Responses[404] = "Service or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(GetServiceByIdRequest request, CancellationToken cancellationToken)
  {
    // Route<T> is used to get values directly from the route within HandleAsync.
    // The request object 'request' will also be populated by FastEndpoints if properties match route parameters.
    var businessProfileIdFromRoute = Route<int>("BusinessProfileId");
    var serviceIdFromRoute = Route<Guid>("ServiceId");

    // Update the request object if it wasn't automatically bound or if you prefer this explicit assignment
    request.BusinessProfileId = businessProfileIdFromRoute;
    request.ServiceId = serviceIdFromRoute;

    // Perform validation (FluentValidation runs before this for the request object itself)
    // Here, we're primarily checking if route parameters are valid if not caught by model binding/route constraints.
    if (businessProfileIdFromRoute <= 0)
    {
      AddError("BusinessProfileId in the route must be a positive integer.", nameof(request.BusinessProfileId));
    }
    if (serviceIdFromRoute == Guid.Empty)
    {
      AddError("ServiceId in the route must be a valid GUID.", nameof(request.ServiceId));
    }
    ThrowIfAnyErrors(); // This will send a 400 if any errors were added.


    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for GetServiceById.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    // Authorization check
    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);
    if (!isAuthorized)
    {
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != businessProfileIdFromRoute)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to get service {ServiceId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          serviceIdFromRoute,
          businessProfileIdFromRoute,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    var query = new GetServiceByIdQuery(serviceIdFromRoute, businessProfileIdFromRoute);
    Result<ServiceDTO> result = await _mediator.Send(query, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        await SendAsync(result.Value, StatusCodes.Status200OK, cancellationToken);
        break;
      case ResultStatus.NotFound:
        await SendNotFoundAsync(cancellationToken);
        break;
      case ResultStatus.Invalid: // Should ideally be caught by request validator, but can handle use case validation
        foreach (var error in result.ValidationErrors)
        {
          AddError(error.ErrorMessage, error.Identifier);
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        break;
      case ResultStatus.Unauthorized:
        await SendUnauthorizedAsync(cancellationToken);
        break;
      case ResultStatus.Forbidden:
        await SendForbiddenAsync(cancellationToken);
        break;
      case ResultStatus.Error:
        Logger.LogError("An error occurred while retrieving service {ServiceId} for BusinessProfileId {BusinessProfileId}: {Errors}", serviceIdFromRoute, businessProfileIdFromRoute, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while retrieving the service.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for GetServiceById with ServiceId {ServiceId}, BusinessProfileId {BusinessProfileId}", result.Status, serviceIdFromRoute, businessProfileIdFromRoute);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
