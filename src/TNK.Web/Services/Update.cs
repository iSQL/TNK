using System.ComponentModel.DataAnnotations; // For [Required] if used in DTO
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles.ADMINISTRATOR
using TNK.Core.Interfaces;
using TNK.UseCases.Services; // For ServiceDTO
using TNK.UseCases.Services.Update; // For UpdateServiceCommand

namespace TNK.Web.Services.Update;

// Request DTO: Combines route parameters and body
public class UpdateServiceRequest
{
  // Route parameters - will be bound by FastEndpoints from the route
  // These are included here primarily for the validator to target them.
  // FastEndpoints will also allow binding them directly in HandleAsync via Route<T>()
  public int BusinessProfileId { get; set; }
  public Guid ServiceId { get; set; }

  // Properties from the request body
  [Required]
  public string? Name { get; set; }
  public string? Description { get; set; }
  [Required]
  public int? DurationInMinutes { get; set; }
  [Required]
  public decimal? Price { get; set; }
  [Required]
  public bool? IsActive { get; set; }
  public string? ImageUrl { get; set; }
}

// Validator for the combined request (route params + body)
public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
  public UpdateServiceRequestValidator()
  {
    // Rules for route parameters
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.ServiceId)
      .NotEmpty().WithMessage("ServiceId is required.");

    // Rules for body parameters
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Service name is required.")
      .MaximumLength(100).WithMessage("Service name cannot exceed 100 characters.");

    RuleFor(x => x.DurationInMinutes)
      .NotNull().WithMessage("Service duration is required.")
      .GreaterThan(0).WithMessage("Service duration must be greater than 0 minutes.");

    RuleFor(x => x.Price)
      .NotNull().WithMessage("Service price is required.")
      .GreaterThanOrEqualTo(0).WithMessage("Service price cannot be negative.");

    RuleFor(x => x.IsActive)
      .NotNull().WithMessage("Active status is required.");

    RuleFor(x => x.Description)
      .MaximumLength(500).WithMessage("Service description cannot exceed 500 characters.")
      .When(x => !string.IsNullOrEmpty(x.Description));

    RuleFor(x => x.ImageUrl)
      .MaximumLength(2048).WithMessage("Image URL is too long.")
      .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
      .When(x => !string.IsNullOrEmpty(x.ImageUrl))
      .WithMessage("Image URL must be a valid absolute URI if provided.");
  }
}

/// <summary>
/// Update an existing Service by its ID, scoped to a Business Profile.
/// </summary>
/// <remarks>
/// Updates an existing service.
/// Accessible by Administrators or the owner of the Business Profile.
/// The request body should contain all fields for the service.
/// </remarks>
public class UpdateServiceEndpoint : Endpoint<UpdateServiceRequest, ServiceDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateServiceEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put("/Businesses/{BusinessProfileId}/Services/{ServiceId:guid}");
    Description(d => d.AutoTagOverride("Services"));
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateServiceRequestValidator>(); // Register the nested validator

    Summary(s =>
    {
      s.Summary = "Update a service by ID.";
      s.Description = "Updates an existing service by its ID, scoped to a business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.ExampleRequest = new UpdateServiceRequest { Name = "Deluxe Manicure", Description = "Full manicure service with polish.", DurationInMinutes = 60, Price = 45.00m, IsActive = true, ImageUrl = null };
      s.Responses[200] = "Service updated successfully, returns the updated service.";
      s.Responses[400] = "Invalid request parameters (validation errors).";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to update this service.";
      s.Responses[404] = "Service or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(UpdateServiceRequest request, CancellationToken cancellationToken)
  {
    // FastEndpoints populates 'request' with both route and body parameters
    // that match property names. We can rely on this for validation.
    // Explicit validation for route params if needed (covered by validator here).
    var businessProfileIdFromRoute = request.BusinessProfileId; // Or Route<int>("BusinessProfileId");
    var serviceIdFromRoute = request.ServiceId; // Or Route<Guid>("ServiceId");

    // FluentValidation for the entire 'request' object (including route params if defined on DTO) runs automatically.
    // If validation fails, a 400 is sent before this handler executes.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for UpdateService.");
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
        Logger.LogWarning("Authorization failed for user {UserId} to update service {ServiceId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          serviceIdFromRoute,
          businessProfileIdFromRoute,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    var command = new UpdateServiceCommand(
      serviceIdFromRoute,
      request.Name!,
      request.Description,
      request.DurationInMinutes!.Value,
      request.Price!.Value,
      request.IsActive!.Value,
      request.ImageUrl,
      businessProfileIdFromRoute // BusinessProfileId for authorization/scoping within the command handler
    );

    Result<ServiceDTO> result = await _mediator.Send(command, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        await SendAsync(result.Value, StatusCodes.Status200OK, cancellationToken);
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
        Logger.LogError("An error occurred while updating service {ServiceId} for BusinessProfileId {BusinessProfileId}: {Errors}", serviceIdFromRoute, businessProfileIdFromRoute, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while updating the service.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for UpdateService with ServiceId {ServiceId}, BusinessProfileId {BusinessProfileId}", result.Status, serviceIdFromRoute, businessProfileIdFromRoute);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
