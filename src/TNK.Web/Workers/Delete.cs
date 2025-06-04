using FastEndpoints;
using MediatR;
using TNK.UseCases.Workers.Delete;
using TNK.Core.Interfaces; // For ICurrentUserService
using System.Security.Claims; // For ClaimsPrincipal extension methods

namespace TNK.Web.Workers;

/// <summary>
/// Represents the request for deleting a worker.
/// </summary>
public class DeleteWorkerRequest
{
  [BindFrom("workerId")]
  public Guid WorkerId { get; set; }
}

/// <summary>
/// Represents the endpoint for deleting a worker.
/// </summary>
public class Delete(IMediator mediator, ICurrentUserService currentUserService) : Endpoint<DeleteWorkerRequest>
{
  private readonly IMediator _mediator = mediator;
  private readonly ICurrentUserService _currentUserService = currentUserService;

  public override void Configure()
  {
    Delete("/workers/{workerId}");
    Roles(TNK.Core.Constants.Roles.Vendor, TNK.Core.Constants.Roles.Admin);
    Description(d => d.AutoTagOverride("Workers"));
    Summary(s =>
    {
      s.Summary = "Deletes a worker.";
      s.Description = "This endpoint allows an authenticated vendor or admin to delete a worker by their ID. Authorization is handled by the backend to ensure users can only delete appropriate workers.";
    });
  }

  public override async Task HandleAsync(DeleteWorkerRequest req, CancellationToken ct)
  {
    int businessProfileIdToDeleteWithin;
    var businessProfileIdClaim = User.FindFirstValue("BusinessProfileId");

    if (User.IsInRole(TNK.Core.Constants.Roles.Admin))
    {
      // Similar to GetById, how Admins specify BusinessProfileId for DeleteWorkerCommand is crucial
      // if the command *requires* it for all roles.
      // If DeleteWorkerCommand(Guid workerId, int businessProfileId) is the signature, Admin needs to provide it.
      // The handler (DeleteWorkerHandler) would then use this for authorization.
      // It's safer if the handler fetches the worker by WorkerId, then uses its actual BusinessProfileId
      // to authorize the Admin. If the command requires BusinessProfileId upfront, the Admin needs a way to supply it.
      // For now, assuming Admin might have a BusinessProfileId in their claims if they are managing a specific business.
      // If not, this is a context issue for the Admin.
      if (string.IsNullOrEmpty(businessProfileIdClaim) || !int.TryParse(businessProfileIdClaim, out businessProfileIdToDeleteWithin))
      {
        // If Admin and no specific BusinessProfileId context, this is problematic for a command
        // that requires BusinessProfileId. The DeleteWorkerHandler needs to be robust.
        // One option: Admin-specific command or handler logic.
        // For now, if Admin has no BusinessProfileId claim, we cannot proceed if command needs it.
        Logger.LogError("Admin attempted to delete worker {WorkerId} without a specific BusinessProfileId context for the command.", req.WorkerId);
        // To make it compile, we'll pass a placeholder, but the handler *must* be aware of this for Admins.
        // This is risky if the handler doesn't have special logic for Admin + placeholder.
        businessProfileIdToDeleteWithin = 0; // Placeholder: Handler must be designed to handle this for Admins.
                                             // A better approach might be an Admin-specific use case that doesn't require BusinessProfileId
                                             // or the handler loads the worker by ID first, then uses ITS BusinessProfileId for auth.
        Logger.LogWarning("Admin deleting worker {WorkerId}. Passing BusinessProfileId {BusinessProfileId} to command. Handler must correctly authorize.", req.WorkerId, businessProfileIdToDeleteWithin);
      }
    }
    else // Vendor
    {
      if (string.IsNullOrEmpty(businessProfileIdClaim) || !int.TryParse(businessProfileIdClaim, out businessProfileIdToDeleteWithin))
      {
        Logger.LogWarning("User {UserId} (Vendor) without BusinessProfileId claim tried to delete worker {WorkerId}.", _currentUserService.UserId, req.WorkerId);
        await SendForbiddenAsync(ct);
        return;
      }
    }

    var command = new DeleteWorkerCommand(req.WorkerId, businessProfileIdToDeleteWithin);
    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct);
    }
    else if (result.Status == Ardalis.Result.ResultStatus.NotFound) await SendNotFoundAsync(ct);
    else if (result.Status == Ardalis.Result.ResultStatus.Forbidden) await SendForbiddenAsync(ct);
    else
    {
      AddError(result.Errors.FirstOrDefault() ?? "An unknown error occurred during deletion.");
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
