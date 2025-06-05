using System; // For Guid
using System.Collections.Generic; // For List
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules; // For ScheduleDTO
using TNK.UseCases.Schedules.ListByWorker; // For ListSchedulesByWorkerQuery

namespace TNK.Web.Schedules.ListByWorker;

// Request DTO: Parameters will be bound from the route
public class ListSchedulesByWorkerRequest
{
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  // Add pagination parameters here if needed in the future
  // public int? PageNumber { get; set; }
  // public int? PageSize { get; set; }
}

// Response DTO
public class ListSchedulesByWorkerResponse
{
  public List<ScheduleDTO> Schedules { get; set; } = new List<ScheduleDTO>();
  // Add pagination metadata here if pagination is implemented
  // public int PageNumber { get; set; }
  // public int PageSize { get; set; }
  // public int TotalRecords { get; set; }
  // public int TotalPages { get; set; }
}

// Validator for route parameters
public class ListSchedulesByWorkerRequestValidator : AbstractValidator<ListSchedulesByWorkerRequest>
{
  public ListSchedulesByWorkerRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.WorkerId)
      .NotEmpty().WithMessage("WorkerId is required.");
  }
}

/// <summary>
/// List all Schedules for a specific Worker within a Business Profile.
/// </summary>
/// <remarks>
/// Retrieves a list of schedules associated with the given WorkerId and BusinessProfileId.
/// Accessible by Administrators or the owner of the Business Profile.
/// </remarks>
public class ListSchedulesByWorkerEndpoint : Endpoint<ListSchedulesByWorkerRequest, ListSchedulesByWorkerResponse>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public ListSchedulesByWorkerEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Get("/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<ListSchedulesByWorkerRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule"));

    Summary(s =>
    {
      s.Summary = "List schedules for a worker.";
      s.Description = "Retrieves all schedules for a specific worker, scoped to a business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[200] = "A list of schedules for the worker.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to access these schedules.";
      s.Responses[404] = "Worker or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(ListSchedulesByWorkerRequest request, CancellationToken cancellationToken)
  {
    // Route parameters are bound to the 'request' DTO by FastEndpoints
    // and validated by ListSchedulesByWorkerRequestValidator automatically.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for ListSchedulesByWorker.");
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
        Logger.LogWarning("Authorization failed for user {UserId} to list schedules for worker {WorkerId} under BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.WorkerId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      // Further check: Does this worker belong to the business?
      // This logic should ideally be part of the ListSchedulesByWorkerQueryHandler.
      isAuthorized = true;
    }

    var query = new ListSchedulesByWorkerQuery(request.WorkerId, request.BusinessProfileId);
    Result<List<ScheduleDTO>> result = await _mediator.Send(query, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        Response = new ListSchedulesByWorkerResponse
        {
          Schedules = result.Value ?? new List<ScheduleDTO>() // Ensure list is not null
          // Populate pagination details if implemented
        };
        await SendAsync(Response, StatusCodes.Status200OK, cancellationToken);
        break;
      case ResultStatus.NotFound:
        // This could mean the Worker or BusinessProfile was not found,
        // or the worker doesn't belong to the business.
        // The query handler should determine the exact reason.
        AddError("The specified worker or business profile was not found, or the worker does not belong to this business.");
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
        Logger.LogError("An error occurred while listing schedules for Worker {WorkerId}, BusinessProfileId {BusinessProfileId}: {Errors}",
            request.WorkerId, request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while listing schedules.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for ListSchedulesByWorker with WorkerId {WorkerId}, BusinessProfileId {BusinessProfileId}",
            result.Status, request.WorkerId, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
