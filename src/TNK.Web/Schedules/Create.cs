using System; // For DateOnly and Guid
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ardalis.Result;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Core.Constants; // For Roles
using TNK.Core.Interfaces; // For ICurrentUserService
using TNK.UseCases.Schedules; // For ScheduleDTO
using TNK.UseCases.Schedules.Create; // For CreateScheduleCommand
// Assuming a GetScheduleByIdEndpoint will exist for the Location header
// Adjust the namespace and class name if your GetById endpoint for schedules is different
using TNK.Web.Schedules.GetById;

namespace TNK.Web.Schedules.Create;

// Request DTO
public class CreateScheduleRequest
{
  // Route parameters will be bound here
  public int BusinessProfileId { get; set; }
  public Guid WorkerId { get; set; } // Changed to Guid

  // Body parameters
  [Required]
  public string? Title { get; set; } // Renamed from Name

  [Required]
  public DateOnly? EffectiveStartDate { get; set; } // Renamed from EffectiveDate

  public DateOnly? EffectiveEndDate { get; set; }

  [Required]
  public string? TimeZoneId { get; set; } // Renamed from TimeZone, e.g., "Europe/Belgrade" or "UTC"

  [Required]
  public bool? IsDefault { get; set; }
}

// Response DTO
public class CreateScheduleResponse
{
  public Guid Id { get; set; }
  public Guid WorkerId { get; set; } // Changed to Guid
  public string Name { get; set; } = string.Empty; // Keeping 'Name' for response consistency, maps from DTO.Title
  public string TimeZone { get; set; } = string.Empty; // Keeping 'TimeZone' for response, maps from DTO.TimeZoneId
  public bool IsDefault { get; set; }
  public DateOnly EffectiveDate { get; set; } // Keeping 'EffectiveDate', maps from DTO.EffectiveStartDate
  public DateOnly? EffectiveEndDate { get; set; }


  // Constructor for when you have the full ScheduleDTO (e.g., if Create command returned it, or after a Get)
  public CreateScheduleResponse(ScheduleDTO dto)
  {
    Id = dto.Id;
    WorkerId = dto.WorkerId;
    Name = dto.Title;
    TimeZone = dto.TimeZoneId;
    IsDefault = dto.IsDefault;
    EffectiveDate = dto.EffectiveStartDate;
    EffectiveEndDate = dto.EffectiveEndDate;
  }

  // Constructor for when command returns only the new ID
  public CreateScheduleResponse(Guid id, Guid workerId, string title, DateOnly effectiveStartDate, DateOnly? effectiveEndDate, string timeZoneId, bool isDefault)
  {
    Id = id;
    WorkerId = workerId;
    Name = title; // Maps from request's Title
    EffectiveDate = effectiveStartDate; // Maps from request's EffectiveStartDate
    EffectiveEndDate = effectiveEndDate;
    TimeZone = timeZoneId; // Maps from request's TimeZoneId
    IsDefault = isDefault;
  }
}

// Validator
public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
  public CreateScheduleRequestValidator()
  {
    RuleFor(x => x.BusinessProfileId)
      .GreaterThan(0).WithMessage("BusinessProfileId from route must be a positive integer.");

    RuleFor(x => x.WorkerId)
      .NotEmpty().WithMessage("WorkerId from route is required."); // Guid validation

    RuleFor(x => x.Title) // Renamed from Name
      .NotEmpty().WithMessage("Schedule title is required.")
      .MaximumLength(100).WithMessage("Schedule title cannot exceed 100 characters.");

    RuleFor(x => x.EffectiveStartDate) // Renamed from EffectiveDate
      .NotNull().WithMessage("Effective start date is required.");

    RuleFor(x => x.EffectiveEndDate)
      .GreaterThanOrEqualTo(x => x.EffectiveStartDate)
      .When(x => x.EffectiveEndDate.HasValue && x.EffectiveStartDate.HasValue)
      .WithMessage("Effective end date must be on or after the effective start date.");

    RuleFor(x => x.TimeZoneId) // Renamed from TimeZone
      .NotEmpty().WithMessage("Time zone ID is required.")
      .MaximumLength(100).WithMessage("Time zone ID cannot exceed 100 characters.");

    RuleFor(x => x.IsDefault)
      .NotNull().WithMessage("IsDefault flag is required.");
  }
}

/// <summary>
/// Create a new Schedule for a Worker within a Business Profile.
/// </summary>
public class CreateScheduleEndpoint : Endpoint<CreateScheduleRequest, CreateScheduleResponse>
{
  private readonly IMediator _mediator;
  private readonly ICurrentUserService _currentUserService;

  public CreateScheduleEndpoint(IMediator mediator, ICurrentUserService currentUserService)
  {
    _mediator = mediator;
    _currentUserService = currentUserService;
  }

  public override void Configure()
  {
    Description(d => d.AutoTagOverride("Schedule"));

    Post("/Businesses/{BusinessProfileId}/Workers/{WorkerId}/Schedules");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Validator<CreateScheduleRequestValidator>();

    Summary(s =>
    {
      s.Summary = "Create a new schedule for a worker.";
      s.Description = "Creates a new schedule for the specified worker, associated with a business profile. Requires authentication and authorization (Admin or Business Owner who owns the worker).";
      s.ExampleRequest = new CreateScheduleRequest { Title = "Default Weekday Schedule", EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow), TimeZoneId = "UTC", IsDefault = true };
      s.Responses[201] = "Schedule created successfully.";
      s.Responses[400] = "Invalid request parameters.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized to create a schedule for this worker/business.";
      s.Responses[404] = "Business profile or worker not found.";
    });
  }

  public override async Task HandleAsync(CreateScheduleRequest request, CancellationToken cancellationToken)
  {
    if (User.Identity == null || !User.Identity.IsAuthenticated)
    {
      Logger.LogWarning("User.Identity is null or user is not authenticated for CreateSchedule.");
      await SendUnauthorizedAsync(cancellationToken);
      return;
    }

    bool isAuthorized = User.IsInRole(Core.Constants.Roles.Admin); // Corrected Role
    if (!isAuthorized)
    {
      int? currentUserBusinessProfileId = _currentUserService.BusinessProfileId;
      if (!currentUserBusinessProfileId.HasValue || currentUserBusinessProfileId.Value != request.BusinessProfileId)
      {
        Logger.LogWarning("Authorization failed for user {UserId} to create schedule for BusinessProfileId {RequestedBusinessProfileId}. User's claimed BusinessProfileId: {UserClaimedBusinessProfileId}",
          _currentUserService.UserId,
          request.BusinessProfileId,
          currentUserBusinessProfileId?.ToString() ?? "null");
        await SendForbiddenAsync(cancellationToken);
        return;
      }
      isAuthorized = true;
    }

    // Command parameters: WorkerId, BusinessProfileId, Title, EffectiveStartDate, EffectiveEndDate, TimeZoneId, IsDefault
    var command = new CreateScheduleCommand(
      request.WorkerId, // Changed to Guid in request
      request.BusinessProfileId,
      request.Title!,
      request.EffectiveStartDate!.Value,
      request.EffectiveEndDate,
      request.TimeZoneId!,
      request.IsDefault!.Value);

    // CreateScheduleCommand returns Result<Guid>
    Result<Guid> result = await _mediator.Send(command, cancellationToken);

    switch (result.Status)
    {
      case ResultStatus.Ok:
      case ResultStatus.Created:
        Guid newScheduleId = result.Value;
        // Construct response from request data and the new ID
        var responseDto = new CreateScheduleResponse(
            newScheduleId,
            request.WorkerId,
            request.Title!,
            request.EffectiveStartDate!.Value,
            request.EffectiveEndDate,
            request.TimeZoneId!,
            request.IsDefault!.Value
        );

        // Ensure GetScheduleByIdEndpoint is correctly named and exists
        await SendCreatedAtAsync<GetScheduleByIdEndpoint>(
            new { request.BusinessProfileId, request.WorkerId, ScheduleId = newScheduleId },
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
      case ResultStatus.NotFound:
        AddError($"The worker with ID {request.WorkerId} or associated business profile was not found.");
        await SendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
        break;
      case ResultStatus.Unauthorized:
        await SendUnauthorizedAsync(cancellationToken);
        break;
      case ResultStatus.Forbidden:
        await SendForbiddenAsync(cancellationToken);
        break;
      case ResultStatus.Error:
        Logger.LogError("An error occurred while creating a schedule for WorkerId {WorkerId} in BusinessProfileId {BusinessProfileId}: {Errors}",
            request.WorkerId, request.BusinessProfileId, string.Join(", ", result.Errors));
        AddError("An unexpected error occurred while creating the schedule.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
      default:
        Logger.LogError("Unhandled ResultStatus {Status} for CreateSchedule with WorkerId {WorkerId}, BusinessProfileId {BusinessProfileId}",
            result.Status, request.WorkerId, request.BusinessProfileId);
        AddError("An unexpected error occurred with unhandled status.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellationToken);
        break;
    }
  }
}
