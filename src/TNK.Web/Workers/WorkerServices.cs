using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants;
using TNK.Core.Interfaces;
using TNK.UseCases.Workers.AssignService;
using TNK.UseCases.Workers.RemoveService;

namespace TNK.Web.WorkerServices;

public static class WorkerServiceRouteConstants
{
  public const string BaseRoute = "/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Services/{ServiceId:guid}";
}

// --- Assign Service to Worker Endpoint ---
public class AssignServiceToWorkerRequest
{
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ServiceId { get; set; }
}

public class AssignServiceToWorkerRequestValidator : AbstractValidator<AssignServiceToWorkerRequest>
{
  public AssignServiceToWorkerRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ServiceId).NotEmpty();
  }
}

public class AssignServiceToWorkerEndpoint : Endpoint<AssignServiceToWorkerRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public AssignServiceToWorkerEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(WorkerServiceRouteConstants.BaseRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<AssignServiceToWorkerRequestValidator>();
    Description(d => d.AutoTagOverride("Worker Service Management"));
    Summary(s =>
    {
      s.Summary = "Assign a service to a worker.";
      s.Description = "Associates a service with a worker, indicating they can perform this service. Requires Admin or Business Owner role.";
      s.Responses[204] = "Service assigned successfully.";
      s.Responses[400] = "Invalid IDs provided.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized.";
      s.Responses[404] = "Worker or Service not found.";
    });
  }

  public override async Task HandleAsync(AssignServiceToWorkerRequest req, CancellationToken ct)
  {
    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);
    if (!isAuthorized)
    {
      var currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != req.BusinessProfileId)
      {
        await SendForbiddenAsync(ct);
        return;
      }
    }

    var command = new AssignServiceToWorkerCommand(req.WorkerId, req.ServiceId, req.BusinessProfileId);
    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, "Error assigning service to worker", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Worker or Service not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, ct);
        break;
      case ResultStatus.Invalid:
        if (validationErrors.Any()) foreach (var valError in validationErrors) AddError(valError.ErrorMessage, valError.Identifier);
        else if (errors.Any()) foreach (var error in errors) AddError(error);
        else AddError("Invalid request parameters.");
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        break;
      case ResultStatus.Forbidden:
        AddError(errors.FirstOrDefault() ?? "Operation forbidden.");
        await SendErrorsAsync(StatusCodes.Status403Forbidden, ct);
        break;
      case ResultStatus.Error:
      default:
        AddError(errors.FirstOrDefault() ?? "An unexpected error occurred.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}

// --- Remove Service from Worker Endpoint ---
public class RemoveServiceFromWorkerRequest
{
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ServiceId { get; set; }
}

public class RemoveServiceFromWorkerRequestValidator : AbstractValidator<RemoveServiceFromWorkerRequest>
{
  public RemoveServiceFromWorkerRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ServiceId).NotEmpty();
  }
}

public class RemoveServiceFromWorkerEndpoint : Endpoint<RemoveServiceFromWorkerRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public RemoveServiceFromWorkerEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete(WorkerServiceRouteConstants.BaseRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<RemoveServiceFromWorkerRequestValidator>();
    Description(d => d.AutoTagOverride("Worker Service Management"));
    Summary(s =>
    {
      s.Summary = "Remove a service from a worker.";
      s.Description = "Disassociates a service from a worker. Requires Admin or Business Owner role.";
      s.Responses[204] = "Service removed successfully from worker.";
    });
  }

  public override async Task HandleAsync(RemoveServiceFromWorkerRequest req, CancellationToken ct)
  {
    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin);
    if (!isAuthorized)
    {
      var currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != req.BusinessProfileId)
      {
        await SendForbiddenAsync(ct);
        return;
      }
    }

    var command = new RemoveServiceFromWorkerCommand(req.WorkerId, req.ServiceId, req.BusinessProfileId);
    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.Errors, "Error removing service from worker", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    // Simplified overload for non-generic Result
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, logContext, ct);
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Worker or Service not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, ct);
        break;
      case ResultStatus.Invalid:
        if (validationErrors.Any()) foreach (var valError in validationErrors) AddError(valError.ErrorMessage, valError.Identifier);
        else if (errors.Any()) foreach (var error in errors) AddError(error);
        else AddError("Invalid request parameters.");
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        break;
      case ResultStatus.Forbidden:
        AddError(errors.FirstOrDefault() ?? "Operation forbidden.");
        await SendErrorsAsync(StatusCodes.Status403Forbidden, ct);
        break;
      case ResultStatus.Error:
      default:
        AddError(errors.FirstOrDefault() ?? "An unexpected error occurred.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}
