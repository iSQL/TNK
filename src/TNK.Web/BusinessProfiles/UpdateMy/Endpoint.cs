using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TNK.Infrastructure.Data;
using TNK.UseCases.BusinessProfiles;
using TNK.UseCases.BusinessProfiles.Update;

namespace TNK.Web.BusinessProfiles.UpdateMy; 

public class UpdateBusinessProfileRequest
{
  // Name can be optional in the request if we allow partial updates
  // but the handler should ensure the entity's Name is not cleared if it's required.
  [MaxLength(200, ErrorMessage = "Business name cannot exceed 200 characters.")]
  public string? Name { get; set; }

  [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
  public string? Address { get; set; }

  [MaxLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
  [Phone(ErrorMessage = "Invalid phone number format.")]
  public string? PhoneNumber { get; set; }

  [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
  public string? Description { get; set; }
}

public class Endpoint : Endpoint<UpdateBusinessProfileRequest, BusinessProfileDTO>
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
    Put("/api/businessprofiles/my");
    Description(d => d.AutoTagOverride("BusinessProfiles"));
    Roles(SeedData.VendorRole);
    Summary(s =>
    {
      s.Summary = "Update the current vendor's business profile";
      s.Description = "Allows an authenticated vendor to update their existing business profile. Only provided fields will be updated.";
      s.ExampleRequest = new UpdateBusinessProfileRequest { Name = "My Updated Salon Name", PhoneNumber = "555-9876" };
      s.Responses[200] = "Business profile updated successfully, returns the updated profile.";
      s.Responses[400] = "Invalid request data or validation error.";
      s.Responses[401] = "User is not authenticated.";
      s.Responses[403] = "User is not authorized (not a Vendor).";
      s.Responses[404] = "Business profile not found for this vendor to update.";
      s.Responses[500] = "An internal server error occurred.";
    });
    Description(d => d
        .Accepts<UpdateBusinessProfileRequest>("application/json")
        .Produces<BusinessProfileDTO>(200, "application/json") 
        .ProducesProblemDetails(400, "application/json")
        .ProducesProblemDetails(404, "application/json")
        .ProducesProblemDetails(500, "application/json")
    );
    Options(x => x.WithName("UpdateMyBusinessProfile"));
  }

  public override async Task HandleAsync(UpdateBusinessProfileRequest req, CancellationToken ct)
  {
    var vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(vendorId))
    {
      _logger.LogWarning("VendorId not found in claims for business profile update.");
      await SendUnauthorizedAsync(ct);
      return;
    }

    var command = new UpdateBusinessProfileCommand(
        vendorId,
        req.Name,
        req.Address,
        req.PhoneNumber,
        req.Description
    );

    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      _logger.LogWarning("Failed to update business profile for VendorId {VendorId}. Status: {Status}, Errors: {Errors}, ValidationErrors: {ValidationErrors}",
          vendorId,
          result.Status,
          string.Join("; ", result.Errors),
          result.ValidationErrorsString()); 

      switch (result.Status)
      {
        case Ardalis.Result.ResultStatus.Invalid:
          foreach (var error in result.ValidationErrors) 
          {
            AddError(error.Identifier, error.ErrorMessage);
          }
          await SendErrorsAsync(400, ct);
          return;
        case Ardalis.Result.ResultStatus.NotFound:
          await SendNotFoundAsync(ct);
          return;
        default: // Error, Unauthorized, etc.
          await SendProblemDetailsAsync(
              title: "Update Failed",
              instance: HttpContext.Request.Path,
              statusCode: 500, // Or map from result.Status if more granular
              detail: result.Errors.FirstOrDefault() ?? "An unexpected error occurred while updating the business profile.",
              cancellation: ct
          );
          return;
      }
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

// Ensure ArdalisResultExtensions is accessible. Defining it here for simplicity.
// For better organization, this could be in a shared utility file/namespace.
public static class ArdalisResultExtensions
{
  public static string ValidationErrorsString(this Ardalis.Result.IResult resultStatus)
  {
    if (resultStatus == null || resultStatus.ValidationErrors == null || !resultStatus.ValidationErrors.Any())
      return string.Empty;
    return string.Join("; ", resultStatus.ValidationErrors.Select(e => $"{e.Identifier}: {e.ErrorMessage}"));
  }
}
