using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles.ADMINISTRATOR
using TNK.Core.Interfaces;
using TNK.UseCases.Services.Delete; // For DeleteServiceCommand

namespace TNK.Web.Services.Delete;

// Request DTO: Parameters will be bound from the route
public class DeleteServiceRequest
{
  // These will be bound from the route: /Businesses/{BusinessProfileId}/Services/{ServiceId}
  public int BusinessProfileId { get; set; }
  public Guid ServiceId { get; set; }
}

// Validator for route parameters
public class DeleteServiceRequestValidator : AbstractValidator<DeleteServiceRequest>
{
  public DeleteServiceRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.ServiceId)
      .NotEmpty().WithMessage("ServiceId is required.");
  }
}

/// <summary>
/// Delete a specific Service by its ID, scoped to a Business Profile.
/// </summary>
/// <remarks>
/// Deletes a specific service.
/// Accessible by Administrators or the owner of the Business Profile.
/// </remarks>
public class DeleteServiceEndpoint : Endpoint<DeleteServiceRequest> // No response body for DELETE
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public DeleteServiceEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete("/Businesses/{BusinessProfileId}/Services/{ServiceId:guid}");
    Description(d => d.AutoTagOverride("Services"));
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<DeleteServiceRequestValidator>(); // Explicitly register the nested validator

    Summary(s =>
    {
      s.Summary = "Delete a service by ID.";
      s.Description = "Deletes a specific service by its ID, scoped to a business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[204] = "Service deleted successfully.";
      s.Responses[400] = "Invalid request parameters (e.g., invalid ID format).";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to delete this service.";
      s.Responses[404] = "Service or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(DeleteServiceRequest request, CancellationToken cancellationToken)
  {
    var businessProfileIdFromRoute = Route<int>("BusinessProfileId");
    var serviceIdFromRoute = Route<Guid>("ServiceId");

    // Update the request object for the validator, though FastEndpoints might do this too
    request.BusinessProfileId = businessProfileIdFromRoute;
    request.ServiceId = serviceIdFromRoute;

    // Initial validation for route parameters (FluentValidation for the 'request' object runs first)
    if (businessProfileIdFromRoute <= 0)
    {
      AddError("BusinessProfileId in the route must be a positive integer.", nameof(request.BusinessProfileId));
    }
    if (serviceIdFromRoute == Guid.Empty)
    {
      AddError("ServiceId in the route must be a valid GUID.", nameof(request.ServiceId));
    }
    ThrowIfAnyErrors(); // Sends 400 if errors were added by AddError

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for DeleteService.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    // Authorization check
    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin); // Using the fully qualified name
    if (!isAuthorized)
    {
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != businessProfileIdFromRoute)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to delete service {ServiceId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          serviceIdFromRoute,
          businessProfileIdFromRoute,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    var command = new DeleteServiceCommand(serviceIdFromRoute, businessProfileIdFromRoute);
    Result result = await _mediator.Send(command, cancellationToken); // Result without a value for Delete

    switch (result.Status)
    {
      case ResultStatus.Ok: // For delete, Ok typically means success (resulting in NoContent)
        await SendNoContentAsync(cancellationToken);
        break;
      case ResultStatus.NotFound:
        await SendNotFoundAsync(cancellationToken);
        break;
      case ResultStatus.Invalid:
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
        Logger.LogError("An error occurred while deleting service {ServiceId} for BusinessProfileId {BusinessProfileId}: {Errors}", serviceIdFromRoute, businessProfileIdFromRoute, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while deleting the service.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for DeleteService with ServiceId {ServiceId}, BusinessProfileId {BusinessProfileId}", result.Status, serviceIdFromRoute, businessProfileIdFromRoute);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
