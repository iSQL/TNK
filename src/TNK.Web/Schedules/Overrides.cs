using System; // For Guid, DateOnly, TimeOnly
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules; // For ScheduleOverrideDTO
using TNK.UseCases.Schedules.Overrides.Add;
using TNK.UseCases.Schedules.Overrides.Remove;
using TNK.UseCases.Schedules.Overrides.Update;
// Assuming GetScheduleByIdEndpoint is for the main schedule.
// For overrides, a specific GetOverrideById might be more appropriate for Location header,
// but using a simpler response for now.
using TNK.Web.Schedules.GetById;


namespace TNK.Web.Schedules.Overrides;

public static class OverrideRouteConstants
{
  public const string BaseRoute = "/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules/{ScheduleId}/Overrides";
  public const string WithOverrideIdRoute = BaseRoute + "/{OverrideId:guid}";
}

// --- 1. Add Schedule Override ---
public class AddScheduleOverrideRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }

  // Body Parameters
  [Required]
  public DateOnly? OverrideDate { get; set; }
  [Required]
  public string? Reason { get; set; }
  [Required]
  public bool? IsWorkingDay { get; set; }

  public TimeOnly? StartTime { get; set; } // Required if IsWorkingDay is true
  public TimeOnly? EndTime { get; set; }   // Required if IsWorkingDay is true
}

public class AddScheduleOverrideRequestValidator : AbstractValidator<AddScheduleOverrideRequest>
{
  public AddScheduleOverrideRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();

    RuleFor(x => x.OverrideDate).NotNull();
    RuleFor(x => x.Reason).NotEmpty().MaximumLength(255);
    RuleFor(x => x.IsWorkingDay).NotNull();

    RuleFor(x => x.StartTime)
        .NotNull().When(x => x.IsWorkingDay == true)
        .WithMessage("Start time is required for a working day override.");

    RuleFor(x => x.EndTime)
        .NotNull().When(x => x.IsWorkingDay == true)
        .WithMessage("End time is required for a working day override.")
        .GreaterThan(x => x.StartTime)
        .When(x => x.IsWorkingDay == true && x.StartTime.HasValue)
        .WithMessage("End time must be after start time for a working day override.");
  }
}

public class AddScheduleOverrideEndpoint : Endpoint<AddScheduleOverrideRequest, ScheduleOverrideDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public AddScheduleOverrideEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(OverrideRouteConstants.BaseRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<AddScheduleOverrideRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Override"));
    Summary(s =>
    {
      s.Summary = "Add an override to a schedule.";
      s.Description = "Adds a specific date override (e.g., a day off, or different working hours) to an existing schedule.";
      s.ExampleRequest = new AddScheduleOverrideRequest { OverrideDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), Reason = "Public Holiday", IsWorkingDay = false };
      s.Responses[201] = "Schedule override added successfully.";
    });
  }

  public override async Task HandleAsync(AddScheduleOverrideRequest req, CancellationToken ct)
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

    var command = new AddScheduleOverrideCommand(
        req.ScheduleId,
        req.WorkerId,
        req.BusinessProfileId,
        req.OverrideDate!.Value,
        req.Reason!,
        req.IsWorkingDay!.Value,
        req.StartTime,
        req.EndTime
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      // For Location header, ideally there's a GetOverrideById endpoint.
      await SendAsync(result.Value, StatusCodes.Status201Created, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified schedule, worker, or business not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, ct);
        break;
      case ResultStatus.Invalid:
        if (validationErrors.Any())
        {
          foreach (var valError in validationErrors) AddError(valError.ErrorMessage, valError.Identifier);
        }
        else if (errors.Any())
        {
          foreach (var error in errors) AddError(error);
        }
        else
        {
          AddError("Invalid request parameters.");
        }
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
        Logger.LogError("Error adding schedule override: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}

// --- 2. Update Schedule Override ---
public class UpdateScheduleOverrideRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid OverrideId { get; set; }

  // Body Parameters
  [Required]
  public DateOnly? OverrideDate { get; set; } // Usually not changed, but included for context/validation.
  [Required]
  public string? Reason { get; set; }
  [Required]
  public bool? IsWorkingDay { get; set; }
  public TimeOnly? StartTime { get; set; }
  public TimeOnly? EndTime { get; set; }
}

public class UpdateScheduleOverrideRequestValidator : AbstractValidator<UpdateScheduleOverrideRequest>
{
  public UpdateScheduleOverrideRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.OverrideId).NotEmpty();

    RuleFor(x => x.OverrideDate).NotNull(); // Date is key, usually not updatable, but required for the command
    RuleFor(x => x.Reason).NotEmpty().MaximumLength(255);
    RuleFor(x => x.IsWorkingDay).NotNull();

    RuleFor(x => x.StartTime)
        .NotNull().When(x => x.IsWorkingDay == true)
        .WithMessage("Start time is required for a working day override.");

    RuleFor(x => x.EndTime)
        .NotNull().When(x => x.IsWorkingDay == true)
        .WithMessage("End time is required for a working day override.")
        .GreaterThan(x => x.StartTime)
        .When(x => x.IsWorkingDay == true && x.StartTime.HasValue)
        .WithMessage("End time must be after start time.");
  }
}

public class UpdateScheduleOverrideEndpoint : Endpoint<UpdateScheduleOverrideRequest, ScheduleOverrideDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateScheduleOverrideEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put(OverrideRouteConstants.WithOverrideIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateScheduleOverrideRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Override"));
    Summary(s => {
      s.Summary = "Update a schedule override.";
      s.Description = "Updates an existing schedule override.";
      s.ExampleRequest = new UpdateScheduleOverrideRequest { OverrideDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), Reason = "Updated Reason", IsWorkingDay = true, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(16, 0) };
      s.Responses[200] = "Schedule override updated successfully.";
    });
  }

  public override async Task HandleAsync(UpdateScheduleOverrideRequest req, CancellationToken ct)
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

    var command = new UpdateScheduleOverrideCommand(
        req.ScheduleId,
        req.OverrideId,
        req.WorkerId,
        req.BusinessProfileId,
        req.OverrideDate!.Value, // Included for context, even if not "updated" by command logic
        req.Reason!,
        req.IsWorkingDay!.Value,
        req.StartTime,
        req.EndTime
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified schedule override, schedule, worker, or business not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, ct);
        break;
      case ResultStatus.Invalid:
        if (validationErrors.Any())
        {
          foreach (var valError in validationErrors) AddError(valError.ErrorMessage, valError.Identifier);
        }
        else if (errors.Any())
        {
          foreach (var error in errors) AddError(error);
        }
        else
        {
          AddError("Invalid request parameters.");
        }
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
        Logger.LogError("Error updating schedule override: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}

// --- 3. Remove Schedule Override ---
public class RemoveScheduleOverrideRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid OverrideId { get; set; }
}

public class RemoveScheduleOverrideRequestValidator : AbstractValidator<RemoveScheduleOverrideRequest>
{
  public RemoveScheduleOverrideRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.OverrideId).NotEmpty();
  }
}

public class RemoveScheduleOverrideEndpoint : Endpoint<RemoveScheduleOverrideRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public RemoveScheduleOverrideEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete(OverrideRouteConstants.WithOverrideIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<RemoveScheduleOverrideRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Override"));
    Summary(s => {
      s.Summary = "Remove an override from a schedule.";
      s.Description = "Deletes a specific override from a schedule.";
      s.Responses[204] = "Schedule override removed successfully.";
    });
  }

  public override async Task HandleAsync(RemoveScheduleOverrideRequest req, CancellationToken ct)
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

    var command = new RemoveScheduleOverrideCommand(
        req.ScheduleId,
        req.OverrideId,
        req.WorkerId,
        req.BusinessProfileId
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.Errors, ct);
    }
  }
  private async Task HandleErrorResult(ResultStatus status, IEnumerable<string> errors, CancellationToken ct)
  {
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, ct);
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified schedule override, schedule, worker, or business not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, ct);
        break;
      case ResultStatus.Forbidden:
        AddError(errors.FirstOrDefault() ?? "Operation forbidden.");
        await SendErrorsAsync(StatusCodes.Status403Forbidden, ct);
        break;
      case ResultStatus.Invalid:
        if (validationErrors.Any())
        {
          foreach (var valError in validationErrors) AddError(valError.ErrorMessage, valError.Identifier);
        }
        else if (errors.Any())
        {
          foreach (var error in errors) AddError(error);
        }
        else
        {
          AddError("Invalid request for deletion.");
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        break;
      case ResultStatus.Error:
      default:
        AddError(errors.FirstOrDefault() ?? "An unexpected error occurred.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        Logger.LogError("Error removing schedule override: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}
