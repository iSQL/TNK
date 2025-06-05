using System; // For Guid, DateOnly
using System.ComponentModel.DataAnnotations; // For [Required]
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules; // For ScheduleDTO
using TNK.UseCases.Schedules.UpdateInfo; // For UpdateScheduleInfoCommand

namespace TNK.Web.Schedules.UpdateInfo;

// Request DTO: Combines route parameters and body
public class UpdateScheduleInfoRequest
{
  // Route parameters - will be bound by FastEndpoints
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; }
  public Guid ScheduleId { get; set; }

  // Body parameters
  [Required]
  public string? Title { get; set; }
  [Required]
  public DateOnly? EffectiveStartDate { get; set; }
  public DateOnly? EffectiveEndDate { get; set; }
  [Required]
  public string? TimeZoneId { get; set; }
  [Required]
  public bool? IsDefault { get; set; }
}

// Validator for the combined request
public class UpdateScheduleInfoRequestValidator : AbstractValidator<UpdateScheduleInfoRequest>
{
  public UpdateScheduleInfoRequestValidator()
  {
    // Rules for route parameters
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId must be a positive integer.");
    RuleFor(x => x.WorkerId)
      .NotEmpty().WithMessage("WorkerId is required.");
    RuleFor(x => x.ScheduleId)
      .NotEmpty().WithMessage("ScheduleId is required.");

    // Rules for body parameters
    RuleFor(x => x.Title)
      .NotEmpty().WithMessage("Schedule title is required.")
      .MaximumLength(100).WithMessage("Schedule title cannot exceed 100 characters.");

    RuleFor(x => x.EffectiveStartDate)
      .NotNull().WithMessage("Effective start date is required.");

    RuleFor(x => x.EffectiveEndDate)
      .GreaterThanOrEqualTo(x => x.EffectiveStartDate)
      .When(x => x.EffectiveEndDate.HasValue && x.EffectiveStartDate.HasValue)
      .WithMessage("Effective end date must be on or after the effective start date.");

    RuleFor(x => x.TimeZoneId)
      .NotEmpty().WithMessage("Time zone ID is required.")
      .MaximumLength(100).WithMessage("Time zone ID cannot exceed 100 characters.");

    RuleFor(x => x.IsDefault)
      .NotNull().WithMessage("IsDefault flag is required.");
  }
}

/// <summary>
/// Update the basic information of an existing Schedule.
/// </summary>
/// <remarks>
/// Updates properties like Title, Effective Dates, TimeZone, and IsDefault status.
/// Accessible by Administrators or the owner of the Business Profile.
/// </remarks>
public class UpdateScheduleInfoEndpoint : Endpoint<UpdateScheduleInfoRequest, ScheduleDTO>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public UpdateScheduleInfoEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Put("/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules/{ScheduleId:guid}");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<UpdateScheduleInfoRequestValidator>();
    Description(d => d.AutoTagOverride("Schedule")); // Added as requested

    Summary(s =>
    {
      s.Summary = "Update schedule information.";
      s.Description = "Updates the core information of a schedule. Requires authentication and authorization (Admin or Business Owner).";
      s.ExampleRequest = new UpdateScheduleInfoRequest { Title = "Updated Weekday Schedule", EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), TimeZoneId = "America/New_York", IsDefault = false };
      s.Responses[200] = "Schedule information updated successfully, returns the updated schedule.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to update this schedule.";
      s.Responses[404] = "Schedule, Worker, or Business Profile not found.";
    });
  }

  public override async Task HandleAsync(UpdateScheduleInfoRequest request, CancellationToken cancellationToken)
  {
    // FastEndpoints populates 'request' with both route and body parameters
    // The validator has already run for the 'request' object.

    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for UpdateScheduleInfo.");
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
        Logger.LogWarning("Authorization failed for user {UserId} to update schedule {ScheduleId} for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.ScheduleId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      // Further check: Does this worker belong to the business?
      // This logic should ideally be part of the UpdateScheduleInfoCommandHandler.
      isAuthorized = true;
    }

    var command = new UpdateScheduleInfoCommand(
      request.ScheduleId,
      request.WorkerId,
      request.BusinessProfileId,
      request.Title!,
      request.EffectiveStartDate!.Value,
      request.EffectiveEndDate,
      request.TimeZoneId!,
      request.IsDefault!.Value
    );

    Result<ScheduleDTO> result = await _mediator.Send(command, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
        await SendAsync(result.Value, StatusCodes.Status200OK, cancellationToken);
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
        Logger.LogError("An error occurred while updating schedule info for {ScheduleId}, Worker {WorkerId}, BusinessProfileId {BusinessProfileId}: {Errors}",
            request.ScheduleId, request.WorkerId, request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while updating the schedule information.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for UpdateScheduleInfo with ScheduleId {ScheduleId}, WorkerId {WorkerId}, BusinessProfileId {BusinessProfileId}",
            result.Status, request.ScheduleId, request.WorkerId, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
