using System; // For Guid
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules; // For ScheduleDTO
using TNK.UseCases.Schedules.GetById; // For GetScheduleByIdQuery

namespace TNK.Web.Schedules.GetById;

// Request DTO: Parameters will be bound from the route
public class GetScheduleByIdRequest
{
  public int BusinessProfileId { get; set; }
  public int WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
}

// Validator for route parameters
public class GetScheduleByIdRequestValidator : AbstractValidator<GetScheduleByIdRequest>
{
  public GetScheduleByIdRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.WorkerId)
      .GreaterThan(0).WithMessage("WorkerId must be a positive integer.");

    RuleFor(x => x.ScheduleId)
      .NotEmpty().WithMessage("ScheduleId is required.");
  }
}

/// <summary>
/// Get a specific Schedule by its ID, scoped to a Worker and Business Profile.
/// </summary>
/// <remarks>
/// Retrieves details for a specific schedule.
/// Accessible by Administrators or the owner of the Business Profile to which the worker belongs.
/// </remarks>
public class GetScheduleByIdEndpoint : Endpoint<GetScheduleByIdRequest, ScheduleDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public GetScheduleByIdEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Description(d => d.AutoTagOverride("Schedule"));
    Get("/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules/{ScheduleId:guid}");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<GetScheduleByIdRequestValidator>();

    Summary(s =>
    {
      s.Summary = "Get a schedule by ID.";
      s.Description = "Retrieves a specific schedule by its ID, scoped to a worker and business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[200] = "The requested schedule details.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to access this schedule.";
      s.Responses[404] = "Schedule, Worker, or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(GetScheduleByIdRequest request, CancellationToken cancellationToken)
  {
    // Route parameters are bound to the 'request' DTO by FastEndpoints
    // and validated by GetScheduleByIdRequestValidator automatically.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for GetScheduleById.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    // Authorization check
    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);
    if (!isAuthorized)
    {
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != request.BusinessProfileId)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to get schedule {ScheduleId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.ScheduleId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      // Further check: Does this worker belong to the business?
      // This logic should ideally be part of the GetScheduleByIdQueryHandler or a shared service.
      // The Query itself doesn't take WorkerId, so the handler needs to validate it if necessary
      // based on the Schedule retrieved by ScheduleId and scoped by BusinessProfileId.
      isAuthorized = true;
    }

    // Corrected instantiation of GetScheduleByIdQuery
    var query = new GetScheduleByIdQuery(request.ScheduleId, request.BusinessProfileId);
    Result<ScheduleDTO> result = await _mediator.Send(query, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        await SendAsync(result.Value, StatusCodes.Status200OK, cancellationToken);
        break;
      case ResultStatus.NotFound:
        // This could mean the Schedule or BusinessProfile was not found,
        // or the Schedule does not belong to the BusinessProfile.
        // The query handler should ensure this.
        AddError("The requested schedule was not found for the specified business profile, or the business profile itself was not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
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
        Logger.LogError("An error occurred while retrieving schedule {ScheduleId} for BusinessProfileId {BusinessProfileId} (WorkerId from route: {WorkerId}): {Errors}",
            request.ScheduleId, request.BusinessProfileId, request.WorkerId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while retrieving the schedule.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for GetScheduleById with ScheduleId {ScheduleId}, BusinessProfileId {BusinessProfileId} (WorkerId from route: {WorkerId})",
            result.Status, request.ScheduleId, request.BusinessProfileId, request.WorkerId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
