using FastEndpoints;
using MediatR;
// Removed TNK.Core.Constants direct using, will use fully qualified names
using TNK.UseCases.Workers.Create;
using TNK.UseCases.Workers.GetById; // Required to fetch the DTO after creation
using TNK.UseCases.Workers;
// Assuming ICurrentUserService is available for fetching user-specific claims if needed indirectly
// using TNK.Core.Interfaces; 

namespace TNK.Web.Workers;

/// <summary>
/// Request to create a new worker.
/// </summary>
public class CreateWorkerRequest
{
  public int BusinessProfileId { get; set; } // This should match the authenticated Vendor's BusinessProfileId
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public string? Email { get; set; }
  public string? PhoneNumber { get; set; }
  public string? ImageUrl { get; set; }
  public string? Specialization { get; set; }
  public string? ApplicationUserId { get; set; }
}

/// <summary>
/// Represents the endpoint for creating a new worker.
/// </summary>
public class Create(IMediator mediator) : Endpoint<CreateWorkerRequest, WorkerDTO>
{
  private readonly IMediator _mediator = mediator;
  // private readonly ICurrentUserService _currentUserService; // Inject if needed for BusinessProfileId validation

  public override void Configure()
  {
    Post("/workers");
    Roles(TNK.Core.Constants.Roles.Vendor); // Fully qualified name
    Description(d => d.AutoTagOverride("Workers"));
    Summary(s =>
    {
      s.Summary = "Creates a new worker.";
      s.Description = "This endpoint allows an authenticated vendor to create a new worker associated with their business profile. The BusinessProfileId in the request should match the vendor's claimed BusinessProfileId.";
      s.ExampleRequest = new CreateWorkerRequest { BusinessProfileId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", PhoneNumber = "1234567890" };
      s.ResponseExamples[201] = new WorkerDTO(Guid.NewGuid(), 1, null, "John", "Doe", "John Doe", "john.doe@example.com", "1234567890", true, null, null);
    });
  }

  public override async Task HandleAsync(CreateWorkerRequest req, CancellationToken ct)
  {
    // Optional: Validate that req.BusinessProfileId matches the authenticated user's BusinessProfileId
    // var currentUserBusinessProfileId = _currentUserService.BusinessProfileId; // Example
    // if (currentUserBusinessProfileId != req.BusinessProfileId)
    // {
    //   await SendForbiddenAsync(ct);
    //   return;
    // }

    var createCommand = new CreateWorkerCommand(
        req.BusinessProfileId,
        req.FirstName,
        req.LastName,
        req.Email,
        req.PhoneNumber,
        req.ImageUrl,
        req.Specialization,
        req.ApplicationUserId
    );

    var createResult = await _mediator.Send(createCommand, ct);

    if (!createResult.IsSuccess)
    {
      AddError(createResult.Errors.FirstOrDefault() ?? "An unknown error occurred during worker creation.");
      await SendErrorsAsync(cancellation: ct);
      return;
    }

    // Fetch the created worker to return its DTO
    // GetWorkerByIdQuery requires BusinessProfileId, use the one from the request
    var getQuery = new GetWorkerByIdQuery(createResult.Value, req.BusinessProfileId);
    var getResult = await _mediator.Send(getQuery, ct);

    if (getResult.IsSuccess)
    {
      // The type `GetById` refers to the TNK.Web.Workers.GetById endpoint class in this namespace.
      await SendCreatedAtAsync<GetById>(new { WorkerId = getResult.Value.Id }, getResult.Value, generateAbsoluteUrl: true, cancellation: ct);
    }
    else
    {
      Logger.LogWarning("Could not retrieve worker DTO for WorkerId {WorkerId} and BusinessProfileId {BusinessProfileId} immediately after creation.", createResult.Value, req.BusinessProfileId);
      AddError("Worker created but could not be retrieved to confirm details.");
      await SendErrorsAsync(statusCode: 500, cancellation: ct);
    }
  }
}
