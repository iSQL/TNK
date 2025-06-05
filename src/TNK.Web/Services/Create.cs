using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using TNK.Core.Constants;
using TNK.Core.Interfaces;
using TNK.UseCases.Services.Create;
using TNK.Web.Services.GetById; // For SendCreatedAtAsync, assuming GetServiceByIdEndpoint is the name

namespace TNK.Web.Services.Create;

// Request DTO - now includes the route parameter
public class CreateServiceRequest
{
  // This will be bound from the route parameter {BusinessProfileId}
  // FastEndpoints automatically binds route parameters to request DTO properties if names match.

  [BindFrom("BusinessProfileId")] // Matches route parameter
  public int BusinessProfileId { get; set; }

  // These are from the request body
  [Required]
  public string? Name { get; set; }
  public string? Description { get; set; }
  [Required]
  public int? DurationInMinutes { get; set; }
  [Required]
  public decimal? Price { get; set; }
  public string? ImageUrl { get; set; }
}

// Response DTO
public class CreateServiceResponse(Guid serviceId, string name)
{
  public Guid ServiceId { get; set; } = serviceId;
  public string Name { get; set; } = name;
}

// Validator - now also validates BusinessProfileId from the request DTO
public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
  public CreateServiceRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId from route must be a positive integer.");

    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Service name is required.")
      .MaximumLength(100).WithMessage("Service name cannot exceed 100 characters.");

    RuleFor(x => x.DurationInMinutes)
      .NotNull().WithMessage("Service duration is required.")
      .GreaterThan(0).WithMessage("Service duration must be greater than 0 minutes.");

    RuleFor(x => x.Price)
      .NotNull().WithMessage("Service price is required.")
      .GreaterThanOrEqualTo(0).WithMessage("Service price cannot be negative.");

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
/// Create a new Service for a Business Profile.
/// </summary>
public class CreateServiceEndpoint : Endpoint<CreateServiceRequest, CreateServiceResponse>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public CreateServiceEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post("/Businesses/{BusinessProfileId}/Services");
    Description(d => d.AutoTagOverride("Services"));

    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<CreateServiceRequestValidator>();

    Summary(s =>
    {
      s.Summary = "Create a new service.";
      s.Description = "Creates a new service for the specified business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.ExampleRequest = new CreateServiceRequest { BusinessProfileId = 1, Name = "Standard Haircut", DurationInMinutes = 30, Price = 25.00m }; // Example includes BusinessProfileId for clarity
      s.Responses[201] = "Service created successfully.";
      s.Responses[400] = "Invalid request parameters (validation errors).";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to create a service for this business profile.";
    });
  }

  public override async Task HandleAsync(CreateServiceRequest request, CancellationToken cancellationToken)
  {
    // 'request.BusinessProfileId' is now populated by FastEndpoints from the route.
    // The validator (CreateServiceRequestValidator) has already run for the entire 'request' object.
    // If validation failed (e.g., BusinessProfileId <= 0), HandleAsync would not be reached,
    // or a 400 Bad Request would have been sent by FastEndpoints.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for CreateService.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    // Authorization check (using request.BusinessProfileId which was bound from the route)
    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);

    if (!isAuthorized)
    {
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != request.BusinessProfileId)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to create service for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    var command = new CreateServiceCommand(
      request.BusinessProfileId, // Use the property from the request DTO
      request.Name!,
      request.Description,
      request.DurationInMinutes!.Value,
      request.Price!.Value,
      request.ImageUrl);

    Result<Guid> result = await _mediator.Send(command, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
      case ResultStatus.Created:
        var responseDto = new CreateServiceResponse(result.Value, request.Name!);
        await SendCreatedAtAsync<GetServiceByIdEndpoint>(
           new { BusinessProfileId = request.BusinessProfileId, ServiceId = result.Value },
           responseDto,
           generateAbsoluteUrl: true,
           cancellation: cancellationToken);
        break;
      case ResultStatus.Invalid:
        foreach (var error in result.ValidationErrors)
        {
          AddError(error.ErrorMessage, error.Identifier);
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        break;
      case ResultStatus.NotFound: // e.g. BusinessProfileId for command not found by use case
        AddError($"The business profile with ID {request.BusinessProfileId} was not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
        break;
      case ResultStatus.Unauthorized:
        await SendUnauthorizedAsync(cancellationToken);
        break;
      case ResultStatus.Forbidden:
        await SendForbiddenAsync(cancellationToken);
        break;
      case ResultStatus.Error:
        Logger.LogError("An error occurred while creating a service for BusinessProfileId {BusinessProfileId}: {Errors}", request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while creating the service.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for CreateService with BusinessProfileId {BusinessProfileId}", result.Status, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
