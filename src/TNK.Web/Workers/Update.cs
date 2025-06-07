using FastEndpoints;
using MediatR;
// using TNK.Core.Constants; // Using fully qualified names
using TNK.UseCases.Workers.Update;
using TNK.UseCases.Workers;
using TNK.Core.Interfaces; // For ICurrentUserService
using System.Security.Claims;

namespace TNK.Web.Workers;

/// <summary>
/// Request to update an existing worker.
/// </summary>
public class UpdateWorkerRequest
{
  [BindFrom("WorkerId")] // From route
  public Guid WorkerId { get; set; }

  // Body parameters
  public int BusinessProfileId { get; set; } // Must match the authenticated Vendor's BusinessProfileId
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public string? Email { get; set; }
  public string? PhoneNumber { get; set; }
  public bool IsActive { get; set; }
  public string? ImageUrl { get; set; }
  public string? Specialization { get; set; }
  public string? ApplicationUserId { get; set; }
}


/// <summary>
/// Represents the endpoint for updating an existing worker.
/// </summary>
public class Update(IMediator mediator, ICurrentUserService currentUserService) : Endpoint<UpdateWorkerRequest, WorkerDTO>
{
  private readonly IMediator _mediator = mediator;
  private readonly ICurrentUserService _currentUserService = currentUserService;


  public override void Configure()
  {
    Put("/workers/{WorkerId}");
    Roles(TNK.Core.Constants.Roles.Vendor, TNK.Core.Constants.Roles.Admin); // Fully qualified names
    Description(d => d.AutoTagOverride("Workers"));
    Summary(s =>
    {
      s.Summary = "Updates an existing worker.";
      s.Description = "This endpoint allows an authenticated vendor or admin to update the details of an existing worker. Vendors must operate on workers within their own BusinessProfileId.";
      s.ExampleRequest = new UpdateWorkerRequest { WorkerId = Guid.NewGuid(), BusinessProfileId = 1, FirstName = "Jane", LastName = "Doe", Email = "jane.doe@example.com", PhoneNumber = "0987654321", IsActive = true };
      s.ResponseExamples[200] = new WorkerDTO(Guid.NewGuid(), 1, null, "Jane", "Doe", "Jane Doe", "jane.doe@example.com", "0987654321", true, null, null, null);
    });
  }

  public override async Task HandleAsync(UpdateWorkerRequest req, CancellationToken ct)
  {
    // Authorization: Ensure the BusinessProfileId in the request matches the user's claim if they are a Vendor.
    // Admins might be exempt or have different validation. This logic is typically in the command handler.
    if (User.IsInRole(TNK.Core.Constants.Roles.Vendor))
    {
      var vendorBusinessProfileIdClaim = User.FindFirstValue("BusinessProfileId");
      if (!int.TryParse(vendorBusinessProfileIdClaim, out var vendorBusinessProfileId) || vendorBusinessProfileId != req.BusinessProfileId)
      {
        AddError("Vendor is not authorized to update worker for the provided BusinessProfileId.");
        await SendForbiddenAsync(ct);
        return;
      }
    }
    // For Admins, the UpdateWorkerCommand handler should verify if the admin is allowed to update this worker.
    // The req.BusinessProfileId is passed to the command for this purpose.

    var command = new UpdateWorkerCommand(
        req.WorkerId,
        req.BusinessProfileId,
        req.FirstName,
        req.LastName,
        req.Email,
        req.PhoneNumber,
        req.IsActive,
        req.ImageUrl,
        req.Specialization,
        req.ApplicationUserId
    );

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else if (result.Status == Ardalis.Result.ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
    }
    else if (result.Status == Ardalis.Result.ResultStatus.Forbidden)
    {
      await SendForbiddenAsync(ct);
    }
    else
    {
      AddError(result.Errors.FirstOrDefault() ?? "An unknown error occurred during update.");
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
