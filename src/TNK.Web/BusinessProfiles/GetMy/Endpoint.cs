using System.Security.Claims;
using TNK.Infrastructure.Data; 
using TNK.UseCases.BusinessProfiles; 
using TNK.UseCases.BusinessProfiles.GetMy; 

namespace TNK.Web.BusinessProfiles.GetMy;

public class Endpoint : EndpointWithoutRequest<BusinessProfileDTO> 
{
  private readonly ISender _mediator;
  private readonly ILogger<Endpoint> _logger;

  public Endpoint(ISender mediator, ILogger<Endpoint> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  public override void Configure()
  {
    Get("/api/businessprofiles/my");
    Description(d => d.AutoTagOverride("BusinessProfiles"));

    Roles(SeedData.VendorRole); 
    Summary(s =>
    {
      s.Summary = "Get the current vendor's business profile";
      s.Description = "Retrieves the business profile associated with the authenticated vendor.";
      s.Responses[200] = "Business profile data returned successfully.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized (not a Vendor).";
      s.Responses[404] = "Business profile not found for this vendor.";
    });
    Options(x => x.WithName("GetMyBusinessProfile"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(vendorId))
    {
      _logger.LogWarning("VendorId not found in claims for GetMyBusinessProfile.");
      await SendUnauthorizedAsync(ct);
      return;
    }

    var query = new GetMyBusinessProfileQuery(vendorId);
    var result = await _mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      if (result.Status == Ardalis.Result.ResultStatus.NotFound)
      {
        await SendNotFoundAsync(ct);
        return;
      }
      _logger.LogWarning("Error fetching business profile for VendorId {VendorId}. Status: {Status}, Errors: {Errors}",
          vendorId, result.Status, string.Join("; ", result.Errors));
      await SendProblemDetailsAsync("Error retrieving profile", HttpContext.Request.Path, 500, result.Errors.FirstOrDefault(), ct);
      return;
    }

    if (result.Value == null) 
    {
      await SendNotFoundAsync(ct);
      return;
    }

    await SendOkAsync(result.Value, ct);
  }

  private Task SendProblemDetailsAsync(string title, string instance, int statusCode, string? detail = null, CancellationToken cancellation = default)
  {
    HttpContext.Response.StatusCode = statusCode;
    return HttpContext.Response.WriteAsJsonAsync(new
    {
      Type = $"https://httpstatuses.com/{statusCode}",
      Title = title,
      Status = statusCode,
      Detail = detail,
      Instance = instance
    },
    cancellation);
  }
}
