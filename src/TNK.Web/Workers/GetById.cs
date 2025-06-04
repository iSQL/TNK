using FastEndpoints;
using MediatR;
// using TNK.Core.Constants; // Using fully qualified names
using TNK.UseCases.Workers.GetById;
using TNK.UseCases.Workers;
using TNK.Core.Interfaces; // For ICurrentUserService
using System.Security.Claims; // For ClaimsPrincipal extension methods if you have them

namespace TNK.Web.Workers;

/// <summary>
/// Represents the request for getting a worker by ID.
/// </summary>
public class GetWorkerByIdRequest
{
  [BindFrom("workerId")]
  public Guid WorkerId { get; set; }
  // BusinessProfileId is not part of the route here, it will be derived from claims or context
}

/// <summary>
/// Represents the endpoint for retrieving a worker by their ID.
/// </summary>
public class GetById(IMediator mediator, ICurrentUserService currentUserService) : Endpoint<GetWorkerByIdRequest, WorkerDTO>
{
  private readonly IMediator _mediator = mediator;
  private readonly ICurrentUserService _currentUserService = currentUserService;

  public override void Configure()
  {
    Get("/workers/{workerId}");
    Roles(TNK.Core.Constants.Roles.Vendor, TNK.Core.Constants.Roles.Admin, TNK.Core.Constants.Roles.Worker); // Fully qualified names
    Description(d => d.AutoTagOverride("Workers"));
    Summary(s =>
    {
      s.Summary = "Retrieves a specific worker by ID.";
      s.Description = "This endpoint returns the details of a worker based on their unique ID. Access is restricted based on user role and association with the business profile.";
    });
  }

  public override async Task HandleAsync(GetWorkerByIdRequest req, CancellationToken ct)
  {
    // BusinessProfileId is required for GetWorkerByIdQuery for authorization.
    // It needs to be determined based on the current user.
    // For a Vendor or Worker, this would be their associated BusinessProfileId.
    // For an Admin, the logic in GetWorkerByIdHandler might allow broader access or require a specific BusinessProfileId if the admin is acting on behalf of one.

    int businessProfileIdToQuery;

    if (User.IsInRole(TNK.Core.Constants.Roles.Admin))
    {
      // For Admins: This is tricky. The query GetWorkerByIdQuery(Guid WorkerId, int BusinessProfileId)
      // is scoped to a BusinessProfileId. If an Admin needs to get ANY worker by ID,
      // the use case might need a different query or the handler needs special logic for Admins.
      // One option: if an Admin is making the call, they might pass BusinessProfileId as a query param (not ideal for this route).
      // Another option: the handler for GetWorkerByIdQuery checks if the user is Admin, and if so,
      // it might ignore the businessProfileId check or fetch the worker and then its BusinessProfileId.
      // For now, let's assume an Admin might need to know the BusinessProfileId or the handler has a way.
      // This part needs clarification based on how Admins are intended to use this.
      // A simple approach for now: if admin, they might not have a single BusinessProfileId claim.
      // This will likely fail if the handler strictly requires a valid BusinessProfileId for admins too without special handling.
      // A possible solution is to have a separate AdminGetWorkerByIdQuery or modify the existing one.
      // For this example, we'll throw if an Admin tries without a clear BusinessProfileId context.
      // Or, more practically, the GetWorkerByIdHandler should fetch the worker by WorkerId first,
      // then use its BusinessProfileId for authorization against the Admin or current user.
      // Let's assume the handler does this: fetches worker by ID, then uses its BPID for auth.
      // So, we can pass a dummy or a specific one if the handler logic supports it for admins.
      // To make this runnable, we'd need a valid BusinessProfileId.
      // This highlights a potential design consideration for admin access patterns.
      // Forcing a placeholder that the handler might ignore for an admin:
      Logger.LogInformation("Admin access for GetWorkerById: BusinessProfileId context needs to be handled by the use case for WorkerId {WorkerId}", req.WorkerId);
      // This is a simplification. The handler should ideally take care of this.
      // If the handler *always* filters by BusinessProfileId, admin needs to provide it.
      // Let's assume for now the handler can deal with it if a "0" or similar is passed for admin,
      // or it fetches the worker by ID then checks ownership against the BusinessProfileId.
      // The most robust way is for the handler to load the worker by ID, then check if the current user (Admin/Vendor/Worker)
      // is authorized for that worker's actual BusinessProfileId.
      // So, the BusinessProfileId passed to the query here for an Admin might be a placeholder if the handler works that way.
      // Let's assume the handler is smart. We'll pass a "0" as a placeholder for admin,
      // expecting the handler to fetch worker by ID first, then use its actual BusinessProfileId for auth.
      // THIS IS A GUESS - THE HANDLER'S LOGIC IS KEY.
      // A safer bet for Admin: they should use an endpoint that allows specifying BusinessProfileId if the query strictly needs it.
      // Given the query signature, we *must* provide one.
      // Let's assume for now, an Admin must operate within a business context if using this specific query.
      // This implies an Admin might select a business to manage first.
      // If not, this endpoint might not be suitable for a generic Admin "get any worker".
      // For the purpose of this example, and to make it compile, we'll try to get it from claims,
      // which might be null for an admin without a specific business context.
      var businessProfileIdClaim = User.FindFirstValue("BusinessProfileId"); // Example claim
      if (string.IsNullOrEmpty(businessProfileIdClaim) || !int.TryParse(businessProfileIdClaim, out businessProfileIdToQuery))
      {
        // If admin and no specific business profile ID is in claims (e.g. super admin)
        // This scenario needs careful design. How does an admin specify which business context?
        // For now, if admin and no BusinessProfileId, we cannot proceed with this query.
        Logger.LogError("Admin attempted to get worker by ID without a specific BusinessProfileId context.");
        await SendForbiddenAsync(ct); // Or Bad Request, as context is missing
        return;
      }
    }
    else // Vendor or Worker
    {
      var businessProfileIdClaim = User.FindFirstValue("BusinessProfileId"); // Example: "BusinessProfileId" claim
      if (string.IsNullOrEmpty(businessProfileIdClaim) || !int.TryParse(businessProfileIdClaim, out businessProfileIdToQuery))
      {
        Logger.LogWarning("User {UserId} without BusinessProfileId claim tried to access worker {WorkerId}.", _currentUserService.UserId, req.WorkerId);
        await SendForbiddenAsync(ct);
        return;
      }
    }

    var query = new GetWorkerByIdQuery(req.WorkerId, businessProfileIdToQuery);
    var result = await _mediator.Send(query, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else
    {
      if (result.Status == Ardalis.Result.ResultStatus.Forbidden)
      {
        await SendForbiddenAsync(ct);
      }
      else
      {
        await SendNotFoundAsync(ct);
      }
    }
  }
}
