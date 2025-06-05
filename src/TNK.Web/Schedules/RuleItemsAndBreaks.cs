using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Ardalis.Result; // Ardalis.Result.ValidationError is here
using FastEndpoints;
using FluentValidation; // For AbstractValidator
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants;
using TNK.Core.Interfaces;
using TNK.UseCases.Schedules;
using TNK.UseCases.Schedules.RuleItems.Add;
using TNK.UseCases.Schedules.RuleItems.Breaks.Add;
using TNK.UseCases.Schedules.RuleItems.Breaks.Remove;
using TNK.UseCases.Schedules.RuleItems.Breaks.Update;
using TNK.UseCases.Schedules.RuleItems.Remove;
using TNK.UseCases.Schedules.RuleItems.Update;
using TNK.Web.Schedules.GetById; // Placeholder for GetById for created resources


// Main namespace for these related endpoints
namespace TNK.Web.Schedules.RuleItemsAndBreaks;

// --- Schedule Rule Item Endpoints ---

public static class RuleItemRouteConstants
{
  public const string BaseRoute = "/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules/{ScheduleId}/RuleItems";
  public const string WithRuleItemIdRoute = BaseRoute + "/{RuleItemId:guid}";
}

// --- 1. Add Schedule Rule Item ---
public class AddScheduleRuleItemRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }

  // Body Parameters
  [Required]
  public DayOfWeek? DayOfWeek { get; set; }
  [Required]
  public TimeOnly? StartTime { get; set; }
  [Required]
  public TimeOnly? EndTime { get; set; }
  [Required]
  public bool? IsWorkingDay { get; set; }
}

public class AddScheduleRuleItemRequestValidator : AbstractValidator<AddScheduleRuleItemRequest>
{
  public AddScheduleRuleItemRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.DayOfWeek).NotNull().IsInEnum();
    RuleFor(x => x.StartTime).NotNull();
    RuleFor(x => x.EndTime).NotNull().GreaterThan(x => x.StartTime)
        .When(x => x.StartTime.HasValue)
        .WithMessage("End time must be after start time.");
    RuleFor(x => x.IsWorkingDay).NotNull();
  }
}

public class AddScheduleRuleItemEndpoint : Endpoint<AddScheduleRuleItemRequest, ScheduleRuleItemDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public AddScheduleRuleItemEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(RuleItemRouteConstants.BaseRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<AddScheduleRuleItemRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Rule Item"));
    Summary(s =>
    {
      s.Summary = "Add a rule item to a schedule.";
      s.Description = "Adds a new recurring rule (e.g., working hours for a specific day) to an existing schedule.";
      s.ExampleRequest = new AddScheduleRuleItemRequest { DayOfWeek = System.DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), IsWorkingDay = true };
      s.Responses[201] = "Rule item added successfully.";
    });
  }

  public override async Task HandleAsync(AddScheduleRuleItemRequest req, CancellationToken ct)
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

    var command = new AddScheduleRuleItemCommand(
        req.ScheduleId,
        req.WorkerId,
        req.BusinessProfileId,
        req.DayOfWeek!.Value,
        req.StartTime!.Value,
        req.EndTime!.Value,
        req.IsWorkingDay!.Value
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
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
        Logger.LogError("Unhandled error in endpoint: {Errors}", string.Join("; ", errors));
        break;
    }
  }
  private async Task HandleErrorResult(ResultStatus status, IEnumerable<string> errors, CancellationToken ct)
  {
    // Pass an empty list for validationErrors as this overload is for non-generic Result
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, ct);
  }
}

// --- 2. Update Schedule Rule Item ---
public class UpdateScheduleRuleItemRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid RuleItemId { get; set; }

  // Body Parameters
  [Required]
  public TimeOnly? StartTime { get; set; }
  [Required]
  public TimeOnly? EndTime { get; set; }
  [Required]
  public bool? IsWorkingDay { get; set; }
}

public class UpdateScheduleRuleItemRequestValidator : AbstractValidator<UpdateScheduleRuleItemRequest>
{
  public UpdateScheduleRuleItemRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.RuleItemId).NotEmpty();
    RuleFor(x => x.StartTime).NotNull();
    RuleFor(x => x.EndTime).NotNull().GreaterThan(x => x.StartTime)
        .When(x => x.StartTime.HasValue)
        .WithMessage("End time must be after start time.");
    RuleFor(x => x.IsWorkingDay).NotNull();
  }
}

public class UpdateScheduleRuleItemEndpoint : Endpoint<UpdateScheduleRuleItemRequest, ScheduleRuleItemDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateScheduleRuleItemEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put(RuleItemRouteConstants.WithRuleItemIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateScheduleRuleItemRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Rule Item"));
    Summary(s => {
      s.Summary = "Update a schedule rule item.";
      s.Description = "Updates an existing rule item (e.g., working hours) within a schedule.";
      s.ExampleRequest = new UpdateScheduleRuleItemRequest { StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(16, 30), IsWorkingDay = true };
      s.Responses[200] = "Rule item updated successfully.";
    });
  }

  public override async Task HandleAsync(UpdateScheduleRuleItemRequest req, CancellationToken ct)
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

    var command = new UpdateScheduleRuleItemCommand(
        req.ScheduleId,
        req.RuleItemId,
        req.WorkerId,
        req.BusinessProfileId,
        req.StartTime!.Value,
        req.EndTime!.Value,
        req.IsWorkingDay!.Value
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
        AddError(errors.FirstOrDefault() ?? "Specified schedule rule item, schedule, worker, or business not found.");
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
        Logger.LogError("Error updating schedule rule item: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}

// --- 3. Remove Schedule Rule Item ---
public class RemoveScheduleRuleItemRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid RuleItemId { get; set; }
}

public class RemoveScheduleRuleItemRequestValidator : AbstractValidator<RemoveScheduleRuleItemRequest>
{
  public RemoveScheduleRuleItemRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.RuleItemId).NotEmpty();
  }
}

public class RemoveScheduleRuleItemEndpoint : Endpoint<RemoveScheduleRuleItemRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public RemoveScheduleRuleItemEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete(RuleItemRouteConstants.WithRuleItemIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<RemoveScheduleRuleItemRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Rule Item"));
    Summary(s => {
      s.Summary = "Remove a rule item from a schedule.";
      s.Description = "Deletes a specific rule item from a schedule.";
      s.Responses[204] = "Rule item removed successfully.";
    });
  }

  public override async Task HandleAsync(RemoveScheduleRuleItemRequest req, CancellationToken ct)
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

    var command = new RemoveScheduleRuleItemCommand(
        req.ScheduleId,
        req.RuleItemId,
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
    // Pass an empty list for validationErrors as this overload is for non-generic Result
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, ct);
  }
  // Keep the more specific overload for consistency, even if not directly used by this non-generic result path
  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified schedule rule item, schedule, worker, or business not found.");
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
        Logger.LogError("Error removing schedule rule item: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}


// --- Schedule Break Rule Endpoints (within a Rule Item) ---

public static class BreakRuleRouteConstants
{
  public const string BaseRoute = RuleItemRouteConstants.WithRuleItemIdRoute + "/Breaks";
  public const string WithBreakIdRoute = BaseRoute + "/{BreakId:guid}";
}

// --- 1. Add Break to Rule Item ---
public class AddBreakToRuleItemRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid RuleItemId { get; set; }

  // Body Parameters
  [Required]
  public string? BreakName { get; set; }
  [Required]
  public TimeOnly? BreakStartTime { get; set; }
  [Required]
  public TimeOnly? BreakEndTime { get; set; }
}

public class AddBreakToRuleItemRequestValidator : AbstractValidator<AddBreakToRuleItemRequest>
{
  public AddBreakToRuleItemRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.RuleItemId).NotEmpty();
    RuleFor(x => x.BreakName).NotEmpty().MaximumLength(100);
    RuleFor(x => x.BreakStartTime).NotNull();
    RuleFor(x => x.BreakEndTime).NotNull().GreaterThan(x => x.BreakStartTime)
        .When(x => x.BreakStartTime.HasValue)
        .WithMessage("Break end time must be after break start time.");
  }
}

public class AddBreakToRuleItemEndpoint : Endpoint<AddBreakToRuleItemRequest, BreakRuleDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public AddBreakToRuleItemEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(BreakRuleRouteConstants.BaseRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<AddBreakToRuleItemRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Break"));
    Summary(s => {
      s.Summary = "Add a break to a schedule rule item.";
      s.Description = "Adds a new break (e.g., lunch break) to a specific rule item within a schedule.";
      s.ExampleRequest = new AddBreakToRuleItemRequest { BreakName = "Lunch", BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0) };
      s.Responses[201] = "Break added successfully.";
    });
  }

  public override async Task HandleAsync(AddBreakToRuleItemRequest req, CancellationToken ct)
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

    var command = new AddBreakToRuleItemCommand(
        req.ScheduleId,
        req.RuleItemId,
        req.WorkerId,
        req.BusinessProfileId,
        req.BreakName!,
        req.BreakStartTime!.Value,
        req.BreakEndTime!.Value
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
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
        AddError(errors.FirstOrDefault() ?? "Specified rule item, schedule, worker, or business not found.");
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
        Logger.LogError("Error adding break to rule item: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}


// --- 2. Update Break in Rule Item ---
public class UpdateBreakRuleRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid RuleItemId { get; set; }
  public Guid BreakId { get; set; }

  // Body Parameters
  [Required]
  public string? BreakName { get; set; }
  [Required]
  public TimeOnly? BreakStartTime { get; set; }
  [Required]
  public TimeOnly? BreakEndTime { get; set; }
}

public class UpdateBreakRuleRequestValidator : AbstractValidator<UpdateBreakRuleRequest>
{
  public UpdateBreakRuleRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.RuleItemId).NotEmpty();
    RuleFor(x => x.BreakId).NotEmpty();
    RuleFor(x => x.BreakName).NotEmpty().MaximumLength(100);
    RuleFor(x => x.BreakStartTime).NotNull();
    RuleFor(x => x.BreakEndTime).NotNull().GreaterThan(x => x.BreakStartTime)
        .When(x => x.BreakStartTime.HasValue)
        .WithMessage("Break end time must be after break start time.");
  }
}

public class UpdateBreakRuleEndpoint : Endpoint<UpdateBreakRuleRequest, BreakRuleDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateBreakRuleEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put(BreakRuleRouteConstants.WithBreakIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateBreakRuleRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Break"));
    Summary(s => {
      s.Summary = "Update a break within a schedule rule item.";
      s.Description = "Updates an existing break for a specific rule item.";
      s.ExampleRequest = new UpdateBreakRuleRequest { BreakName = "Extended Lunch", BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 30) };
      s.Responses[200] = "Break updated successfully.";
    });
  }

  public override async Task HandleAsync(UpdateBreakRuleRequest req, CancellationToken ct)
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

    var command = new UpdateBreakRuleCommand(
        req.ScheduleId,
        req.RuleItemId,
        req.BreakId,
        req.WorkerId,
        req.BusinessProfileId,
        req.BreakName!,
        req.BreakStartTime!.Value,
        req.BreakEndTime!.Value
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
        AddError(errors.FirstOrDefault() ?? "Specified break, rule item, schedule, worker, or business not found.");
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
        Logger.LogError("Error updating break rule: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}

// --- 3. Remove Break from Rule Item ---
public class RemoveBreakRuleRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }
  public Guid RuleItemId { get; set; }
  public Guid BreakId { get; set; }
}

public class RemoveBreakRuleRequestValidator : AbstractValidator<RemoveBreakRuleRequest>
{
  public RemoveBreakRuleRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.ScheduleId).NotEmpty();
    RuleFor(x => x.RuleItemId).NotEmpty();
    RuleFor(x => x.BreakId).NotEmpty();
  }
}

public class RemoveBreakRuleEndpoint : Endpoint<RemoveBreakRuleRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public RemoveBreakRuleEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete(BreakRuleRouteConstants.WithBreakIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<RemoveBreakRuleRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule Break"));
    Summary(s => {
      s.Summary = "Remove a break from a schedule rule item.";
      s.Description = "Deletes a specific break from a rule item.";
      s.Responses[204] = "Break removed successfully.";
    });
  }

  public override async Task HandleAsync(RemoveBreakRuleRequest req, CancellationToken ct)
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

    var command = new RemoveBreakRuleCommand(
        req.ScheduleId,
        req.RuleItemId,
        req.BreakId,
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
    // Pass an empty list for validationErrors as this overload is for non-generic Result
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, ct);
  }
  // Keep the more specific overload for consistency
  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified break, rule item, schedule, worker, or business not found.");
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
        Logger.LogError("Error removing break rule: {Errors}", string.Join("; ", errors));
        break;
    }
  }
}
