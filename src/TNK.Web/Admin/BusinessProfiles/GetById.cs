using Microsoft.AspNetCore.Mvc;
using TNK.Infrastructure.Data; 
using TNK.UseCases.BusinessProfiles; 
using TNK.UseCases.BusinessProfiles.GetByIdAdmin;

namespace TNK.Web.Admin.BusinessProfiles;

/// <summary>
/// Request DTO for getting a Business Profile by ID (Admin).
/// </summary>
public record GetByIdAdminRequest
{
  /// <summary>
  /// The ID of the Business Profile to retrieve.
  /// </summary>
  [FromRoute] // Binds from the route parameter
  public int BusinessProfileId { get; init; }
}

/// <summary>
/// API endpoint for SuperAdmins to get a specific Business Profile by its ID.
/// </summary>
public class GetById : Endpoint<GetByIdAdminRequest, BusinessProfileDTO>
{
  private readonly ISender _sender; // MediatR ISender

  public GetById(ISender sender)
  {
    _sender = sender;
  }

  public override void Configure()
  {
    Get("/api/admin/businessprofiles/{BusinessProfileId}");
    Description(d => d.AutoTagOverride("Admin_BusinessProfiles"));
    AuthSchemes(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme); 
    Roles(SeedData.AdminRole); 
    Summary(s =>
    {
      s.Summary = "Get a specific Business Profile by ID (Admin)";
      s.Description = "Allows a SuperAdmin to retrieve the details of a specific business profile.";
      s.Response<BusinessProfileDTO>(200, "Business profile found and returned.");
      s.Response(401, "Unauthorized if the user is not authenticated.");
      s.Response(403, "Forbidden if the user is not a SuperAdmin.");
      s.Response(404, "Not Found if the business profile does not exist.");
    });
    Tags("Admin_BusinessProfiles"); 
  }

  public override async Task HandleAsync(GetByIdAdminRequest req, CancellationToken ct)
  {
    var query = new GetBusinessProfileByIdAdminQuery(req.BusinessProfileId);
    var result = await _sender.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else 
    {
      AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred.");
      if (result.ValidationErrors.Any())
      {
        AddError(result.ValidationErrors.First().ErrorMessage);
      }
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
