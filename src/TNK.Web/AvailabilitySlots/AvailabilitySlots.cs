using System; // For Guid, DateTime, DateOnly
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
using TNK.Core.ServiceManagementAggregate.Enums; // For AvailabilitySlotStatus
using TNK.UseCases.AvailabilitySlots; // For AvailabilitySlotDTO
using TNK.UseCases.AvailabilitySlots.CreateManual; // For CreateManualAvailabilitySlotCommand
using TNK.UseCases.AvailabilitySlots.Delete; // For DeleteAvailabilitySlotCommand
using TNK.UseCases.AvailabilitySlots.Generate; // For GenerateAvailabilitySlotsCommand
using TNK.UseCases.AvailabilitySlots.ListByWorkerAndDate; // For ListAvailabilitySlotsQuery
using TNK.UseCases.AvailabilitySlots.Update; // For UpdateAvailabilitySlotCommand
// Placeholder for a GetById endpoint if needed for Location header
// using TNK.Web.AvailabilitySlots.GetById; 

namespace TNK.Web.AvailabilitySlots;

public static class AvailabilitySlotRouteConstants
{
  // Base route for slots related to a worker
  public const string BaseWorkerSlotsRoute = "/Businesses/{BusinessProfileId}/Workers/{WorkerId}/AvailabilitySlots";

  // Specific route for manually created slots under a worker
  public const string CreateManualSlotRoute = BaseWorkerSlotsRoute + "/Manual";

  // Route for a specific slot by ID 
  public const string WithSlotIdRoute = BaseWorkerSlotsRoute + "/{SlotId:guid}";

  // Route for generating slots
  public const string GenerateSlotsRoute = BaseWorkerSlotsRoute + "/Generate";
}

// --- 1. Create Manual Availability Slot ---
public class CreateManualAvailabilitySlotRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }

  // Body Parameters
  [Required]
  public DateTime? StartTime { get; set; }
  [Required]
  public DateTime? EndTime { get; set; }

  public AvailabilitySlotStatus? Status { get; set; }
}

public class CreateManualAvailabilitySlotRequestValidator : AbstractValidator<CreateManualAvailabilitySlotRequest>
{
  public CreateManualAvailabilitySlotRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();

    RuleFor(x => x.StartTime)
        .NotNull().WithMessage("Start time is required.")
        .LessThan(x => x.EndTime).When(x => x.EndTime.HasValue)
        .WithMessage("Start time must be before end time.");

    RuleFor(x => x.EndTime)
        .NotNull().WithMessage("End time is required.");

    RuleFor(x => x.Status)
        .IsInEnum().When(x => x.Status.HasValue)
        .WithMessage("Invalid slot status provided.");
  }
}

public class CreateManualAvailabilitySlotEndpoint : Endpoint<CreateManualAvailabilitySlotRequest, AvailabilitySlotDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public CreateManualAvailabilitySlotEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(AvailabilitySlotRouteConstants.CreateManualSlotRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<CreateManualAvailabilitySlotRequestValidator>();
    Description(d => d.AutoTagOverride("Availability Slot"));
    Summary(s =>
    {
      s.Summary = "Manually create an availability slot for a worker.";
      s.Description = "Allows creating a specific available or unavailable time slot for a worker. Requires Admin or Business Owner role.";
      s.ExampleRequest = new CreateManualAvailabilitySlotRequest
      {
        StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
        EndTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
        Status = AvailabilitySlotStatus.Available
      };
      s.Responses[201] = "Availability slot created successfully.";
    });
  }

  public override async Task HandleAsync(CreateManualAvailabilitySlotRequest req, CancellationToken ct)
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

    // Ensure DateTime values are UTC
    DateTime startTimeUtc = req.StartTime!.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(req.StartTime.Value, DateTimeKind.Utc)
                            : req.StartTime.Value.ToUniversalTime();
    DateTime endTimeUtc = req.EndTime!.Value.Kind == DateTimeKind.Unspecified
                          ? DateTime.SpecifyKind(req.EndTime.Value, DateTimeKind.Utc)
                          : req.EndTime.Value.ToUniversalTime();

    var command = new CreateManualAvailabilitySlotCommand(
        req.WorkerId,
        req.BusinessProfileId,
        startTimeUtc,
        endTimeUtc,
        req.Status ?? AvailabilitySlotStatus.Available
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendAsync(result.Value, StatusCodes.Status201Created, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, "Error creating manual availability slot", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified resource not found.");
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
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
  private async Task HandleErrorResult(ResultStatus status, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, logContext, ct);
  }
}

// --- 2. List Availability Slots by Worker and Date Range ---
public class ListAvailabilitySlotsRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }

  // Query Parameters
  [Required]
  public DateTime? StartDate { get; set; }
  [Required]
  public DateTime? EndDate { get; set; }
}

public class ListAvailabilitySlotsResponse
{
  public List<AvailabilitySlotDTO> Slots { get; set; } = new List<AvailabilitySlotDTO>();
}

public class ListAvailabilitySlotsRequestValidator : AbstractValidator<ListAvailabilitySlotsRequest>
{
  public ListAvailabilitySlotsRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();

    RuleFor(x => x.StartDate)
        .NotNull().WithMessage("Start date is required.");

    RuleFor(x => x.EndDate)
        .NotNull().WithMessage("End date is required.")
        .GreaterThanOrEqualTo(x => x.StartDate).When(x => x.StartDate.HasValue)
        .WithMessage("End date must be on or after start date.");
  }
}

public class ListAvailabilitySlotsEndpoint : Endpoint<ListAvailabilitySlotsRequest, ListAvailabilitySlotsResponse>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public ListAvailabilitySlotsEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Get(AvailabilitySlotRouteConstants.BaseWorkerSlotsRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<ListAvailabilitySlotsRequestValidator>();
    Description(d => d.AutoTagOverride("Availability Slot"));
    Summary(s =>
    {
      s.Summary = "List availability slots for a worker within a date range.";
      s.Description = "Retrieves available and unavailable time slots for a worker. Requires Admin or Business Owner role.";
      s.ExampleRequest = new ListAvailabilitySlotsRequest
      {
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(7)
      };
      s.Responses[200] = "A list of availability slots.";
    });
  }

  public override async Task HandleAsync(ListAvailabilitySlotsRequest req, CancellationToken ct)
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

    // Ensure DateTime values are UTC before passing to the query
    DateTime startDateUtc = req.StartDate!.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(req.StartDate.Value, DateTimeKind.Utc)
                            : req.StartDate.Value.ToUniversalTime();
    DateTime endDateUtc = req.EndDate!.Value.Kind == DateTimeKind.Unspecified
                          ? DateTime.SpecifyKind(req.EndDate.Value, DateTimeKind.Utc)
                          : req.EndDate.Value.ToUniversalTime();
    // For EndDate, if it's just a date, you might want to set it to the end of that day in UTC
    // e.g., endDateUtc = DateTime.SpecifyKind(req.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    // However, ListAvailabilitySlotsQuery expects DateTime, so we'll pass it as is (now UTC).
    // The handler for ListAvailabilitySlotsQuery should be aware of how to interpret this range.


    var query = new ListAvailabilitySlotsQuery(
        req.WorkerId,
        req.BusinessProfileId,
        startDateUtc,
        endDateUtc
    );

    var result = await _mediator.Send(query, ct);

    if (result.IsSuccess)
    {
      Response = new ListAvailabilitySlotsResponse { Slots = result.Value ?? new List<AvailabilitySlotDTO>() };
      await SendOkAsync(Response, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, "Error listing availability slots", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified worker or business profile not found, or no slots found for the criteria.");
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
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}

// --- 3. Update Availability Slot ---
public class UpdateAvailabilitySlotRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid SlotId { get; set; } // AvailabilitySlotId from route

  // Body Parameters
  public DateTime? NewStartTime { get; set; }
  public DateTime? NewEndTime { get; set; }
  public AvailabilitySlotStatus? NewStatus { get; set; }
}

public class UpdateAvailabilitySlotRequestValidator : AbstractValidator<UpdateAvailabilitySlotRequest>
{
  public UpdateAvailabilitySlotRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.SlotId).NotEmpty();

    RuleFor(x => x)
        .Must(x => x.NewStartTime.HasValue || x.NewEndTime.HasValue || x.NewStatus.HasValue)
        .WithMessage("At least one field (NewStartTime, NewEndTime, or NewStatus) must be provided for update.");

    When(x => x.NewStartTime.HasValue || x.NewEndTime.HasValue, () => {
      RuleFor(x => x.NewStartTime)
          .NotNull().When(x => x.NewEndTime.HasValue && !x.NewStartTime.HasValue)
          .WithMessage("If NewEndTime is provided, NewStartTime must also be provided or already exist for the slot.")
          .LessThan(x => x.NewEndTime).When(x => x.NewStartTime.HasValue && x.NewEndTime.HasValue)
          .WithMessage("New start time must be before new end time.");

      RuleFor(x => x.NewEndTime)
          .NotNull().When(x => x.NewStartTime.HasValue && !x.NewEndTime.HasValue)
          .WithMessage("If NewStartTime is provided, NewEndTime must also be provided or already exist for the slot.");
    });

    RuleFor(x => x.NewStatus)
        .IsInEnum().When(x => x.NewStatus.HasValue)
        .WithMessage("Invalid slot status provided.");
  }
}

public class UpdateAvailabilitySlotEndpoint : Endpoint<UpdateAvailabilitySlotRequest, AvailabilitySlotDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateAvailabilitySlotEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put(AvailabilitySlotRouteConstants.WithSlotIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateAvailabilitySlotRequestValidator>();
    Description(d => d.AutoTagOverride("Availability Slot"));
    Summary(s =>
    {
      s.Summary = "Update an availability slot.";
      s.Description = "Updates the time and/or status of a manually created or generated availability slot. Requires Admin or Business Owner role.";
      s.ExampleRequest = new UpdateAvailabilitySlotRequest { NewStatus = AvailabilitySlotStatus.Unavailable };
      s.Responses[200] = "Availability slot updated successfully.";
    });
  }

  public override async Task HandleAsync(UpdateAvailabilitySlotRequest req, CancellationToken ct)
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

    DateTime? newStartTimeUtc = null;
    if (req.NewStartTime.HasValue)
    {
      newStartTimeUtc = req.NewStartTime.Value.Kind == DateTimeKind.Unspecified
                          ? DateTime.SpecifyKind(req.NewStartTime.Value, DateTimeKind.Utc)
                          : req.NewStartTime.Value.ToUniversalTime();
    }

    DateTime? newEndTimeUtc = null;
    if (req.NewEndTime.HasValue)
    {
      newEndTimeUtc = req.NewEndTime.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(req.NewEndTime.Value, DateTimeKind.Utc)
                        : req.NewEndTime.Value.ToUniversalTime();
    }

    var command = new UpdateAvailabilitySlotCommand(
        req.SlotId,
        req.BusinessProfileId,
        req.WorkerId,
        newStartTimeUtc,
        newEndTimeUtc,
        req.NewStatus
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, "Error updating availability slot", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Availability slot, worker, or business profile not found.");
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
          AddError("Invalid request parameters for update.");
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
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}

// --- 4. Delete Availability Slot ---
public class DeleteAvailabilitySlotRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid SlotId { get; set; }
}

public class DeleteAvailabilitySlotRequestValidator : AbstractValidator<DeleteAvailabilitySlotRequest>
{
  public DeleteAvailabilitySlotRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.SlotId).NotEmpty();
  }
}

public class DeleteAvailabilitySlotEndpoint : Endpoint<DeleteAvailabilitySlotRequest>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public DeleteAvailabilitySlotEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Delete(AvailabilitySlotRouteConstants.WithSlotIdRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<DeleteAvailabilitySlotRequestValidator>();
    Description(d => d.AutoTagOverride("Availability Slot"));
    Summary(s =>
    {
      s.Summary = "Delete an availability slot.";
      s.Description = "Deletes a specific availability slot. Requires Admin or Business Owner role.";
      s.Responses[204] = "Availability slot deleted successfully.";
    });
  }

  public override async Task HandleAsync(DeleteAvailabilitySlotRequest req, CancellationToken ct)
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

    var command = new DeleteAvailabilitySlotCommand(
        req.SlotId,
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
      await HandleErrorResult(result.Status, result.Errors, "Error deleting availability slot", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    await HandleErrorResult(status, new List<Ardalis.Result.ValidationError>(), errors, logContext, ct);
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Availability slot, worker, or business profile not found.");
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
          AddError("Invalid request for deletion.");
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
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}

// --- 5. Generate Availability Slots ---
public class GenerateAvailabilitySlotsRequest
{
  // Route Parameters
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }

  // Body Parameters
  [Required]
  public DateOnly? StartDate { get; set; }
  [Required]
  public DateOnly? EndDate { get; set; }
  public Guid? ScheduleId { get; set; }
  public int? SlotDurationInMinutes { get; set; }
  public bool? OverwriteExistingGeneratedUnbookedSlots { get; set; }
}

public class GenerateAvailabilitySlotsResponse
{
  public int SlotsGenerated { get; set; }
  public string Message { get; set; } = string.Empty;
}

public class GenerateAvailabilitySlotsRequestValidator : AbstractValidator<GenerateAvailabilitySlotsRequest>
{
  public GenerateAvailabilitySlotsRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId).GreaterThan(0);
    RuleFor(x => x.WorkerId).NotEmpty();
    RuleFor(x => x.StartDate).NotNull();
    RuleFor(x => x.EndDate)
        .NotNull()
        .GreaterThanOrEqualTo(x => x.StartDate).When(x => x.StartDate.HasValue)
        .WithMessage("End date must be on or after start date.");
    RuleFor(x => x.ScheduleId).NotEmpty().When(x => x.ScheduleId.HasValue);
    RuleFor(x => x.SlotDurationInMinutes)
        .GreaterThan(0).When(x => x.SlotDurationInMinutes.HasValue)
        .WithMessage("Slot duration must be greater than 0.");
  }
}

public class GenerateAvailabilitySlotsEndpoint : Endpoint<GenerateAvailabilitySlotsRequest, GenerateAvailabilitySlotsResponse>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public GenerateAvailabilitySlotsEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Post(AvailabilitySlotRouteConstants.GenerateSlotsRoute);
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<GenerateAvailabilitySlotsRequestValidator>();
    Description(d => d.AutoTagOverride("Availability Slot"));
    Summary(s =>
    {
      s.Summary = "Generate availability slots for a worker based on a schedule.";
      s.Description = "Generates time slots for a worker over a date range using their default or a specified schedule. Requires Admin or Business Owner role.";
      s.ExampleRequest = new GenerateAvailabilitySlotsRequest
      {
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8))
      };
      s.Responses[200] = "Slot generation process completed.";
    });
  }

  public override async Task HandleAsync(GenerateAvailabilitySlotsRequest req, CancellationToken ct)
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

    var command = new GenerateAvailabilitySlotsCommand(
        req.WorkerId,
        req.BusinessProfileId,
        req.StartDate!.Value,
        req.EndDate!.Value,
        req.ScheduleId,
        req.SlotDurationInMinutes ?? 30,
        req.OverwriteExistingGeneratedUnbookedSlots ?? true
    );

    Result<int> result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new GenerateAvailabilitySlotsResponse
      {
        SlotsGenerated = result.Value,
        Message = $"{result.Value} availability slots processed/generated successfully."
      };
      await SendOkAsync(Response, ct);
    }
    else
    {
      await HandleErrorResult(result.Status, result.ValidationErrors, result.Errors, "Error generating availability slots", ct);
    }
  }

  private async Task HandleErrorResult(ResultStatus status, IEnumerable<Ardalis.Result.ValidationError> validationErrors, IEnumerable<string> errors, string logContext, CancellationToken ct)
  {
    switch (status)
    {
      case ResultStatus.NotFound:
        AddError(errors.FirstOrDefault() ?? "Specified worker, business profile, or schedule not found.");
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
          AddError("Invalid parameters for slot generation.");
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        break;
      case ResultStatus.Forbidden:
        AddError(errors.FirstOrDefault() ?? "Operation forbidden.");
        await SendErrorsAsync(StatusCodes.Status403Forbidden, ct);
        break;
      case ResultStatus.Error:
      default:
        AddError(errors.FirstOrDefault() ?? "An unexpected error occurred during slot generation.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        Logger.LogError("{Context}: {Errors}", logContext, string.Join("; ", errors));
        break;
    }
  }
}

