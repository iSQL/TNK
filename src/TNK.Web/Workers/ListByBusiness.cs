
using FastEndpoints;
using MediatR;
// using TNK.Core.Constants; // Using fully qualified names
using TNK.UseCases.Workers.ListByBusiness;
using TNK.UseCases.Workers;

namespace TNK.Web.Workers;

/// <summary>
/// Represents the request for listing workers by business.
/// </summary>
public class ListByBusinessRequest
{
  [BindFrom("businessProfileId")] // Matches route parameter
  public int BusinessProfileId { get; set; }
}

/// <summary>
/// Represents the endpoint for listing workers by business.
/// </summary>
public class ListByBusiness(IMediator mediator) : Endpoint<ListByBusinessRequest, IEnumerable<WorkerDTO>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/businesses/{businessProfileId}/workers");
    Roles(TNK.Core.Constants.Roles.Vendor, TNK.Core.Constants.Roles.Admin); // Fully qualified names
    Description(d => d.AutoTagOverride("Workers"));
    Summary(s =>
    {
      s.Summary = "Lists all workers for a given business profile.";
      s.Description = "This endpoint retrieves a list of workers associated with the specified business profile ID. Vendors can only access workers of their own business. Admins can access any.";
    });
  }

  public override async Task HandleAsync(ListByBusinessRequest req, CancellationToken ct)
  {
    // Authorization note: The ListWorkersByBusinessQueryHandler should ensure that
    // a Vendor can only query for their own BusinessProfileId.
    // An Admin might be able to query for any.
    var query = new ListWorkersByBusinessQuery(req.BusinessProfileId);
    var result = await _mediator.Send(query, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else
    {
      AddError(result.Errors.FirstOrDefault() ?? "Failed to retrieve workers.");
      await SendErrorsAsync(cancellation: ct); // Send errors with appropriate status code
    }
  }
}
