using System; // For Guid
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules.Delete; // For DeleteScheduleCommand

namespace TNK.Web.Schedules.Delete;

// Request DTO: Parameters will be bound from the route
public class DeleteScheduleRequest
{
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
}

// Validator for route parameters
public class DeleteScheduleRequestValidator : AbstractValidator<DeleteScheduleRequest>
{
  public DeleteScheduleRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");

    RuleFor(x => x.WorkerId)
      .NotEmpty().WithMessage("WorkerId is required.");

    RuleFor(x => x.ScheduleId)
      .NotEmpty().WithMessage("ScheduleId is required.");
  }
}

/// <summary>
/// Delete a specific Schedule by its ID, scoped to a Worker and Business Profile.
/// </summary>
/// <remarks>
/// Deletes a specific schedule.
/// Accessible by Administrators or the owner of the Business Profile to which the worker belongs.
/// </remarks>
public class DeleteScheduleEndpoint : Endpoint<DeleteScheduleRequest> // No response body for DELETE
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public DeleteScheduleEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Description(d => d.AutoTagOverride("Schedule"));
    Delete("/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules/{ScheduleId:guid}");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<DeleteScheduleRequestValidator>();

    Summary(s =>
    {
      s.Summary = "Delete a schedule by ID.";
      s.Description = "Deletes a specific schedule by its ID, scoped to a worker and business profile. Requires authentication and authorization (Admin or Business Owner).";
      s.Responses[204] = "Schedule deleted successfully.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to delete this schedule.";
      s.Responses[404] = "Schedule, Worker, or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(DeleteScheduleRequest request, CancellationToken cancellationToken)
  {
    // Route parameters are bound to the 'request' DTO by FastEndpoints
    // and validated by DeleteScheduleRequestValidator automatically.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for DeleteSchedule.");
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
        Logger.LogWarning("Authorization failed for user {UserId} to delete schedule {ScheduleId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.ScheduleId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      // Further check: Does this worker belong to the business?
      // This logic should ideally be part of the DeleteScheduleCommandHandler.
      isAuthorized = true;
    }

    var command = new DeleteScheduleCommand(request.ScheduleId, request.WorkerId, request.BusinessProfileId);
    Result result = await _mediator.Send(command, cancellationToken); // Delete commands often return a plain Result

    switch (result.Status)
    {
      case ResultStatus.Ok:
        await SendNoContentAsync(cancellationToken); // 204 No Content for successful DELETE
        break;
      case ResultStatus.NotFound:
        AddError("The requested schedule, worker, or business profile was not found, or the schedule does not belong to the specified worker/business.");
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
        Logger.LogError("An error occurred while deleting schedule {ScheduleId} for Worker {WorkerId}, BusinessProfileId {BusinessProfileId}: {Errors}",
            request.ScheduleId, request.WorkerId, request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while deleting the schedule.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for DeleteSchedule with ScheduleId {ScheduleId}, WorkerId {WorkerId}, BusinessProfileId {BusinessProfileId}",
            result.Status, request.ScheduleId, request.WorkerId, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
