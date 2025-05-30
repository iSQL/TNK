using TNK.Infrastructure.Data; 
using TNK.UseCases.BusinessProfiles;
using TNK.UseCases.BusinessProfiles.ListAdmin; 

namespace TNK.Web.Admin.BusinessProfiles;

/// <summary>
/// Request DTO for listing Business Profiles (Admin) with pagination and search.
/// </summary>
public record ListAdminRequest
{
  /// <summary>
  /// Page number for pagination. Defaults to 1.
  /// </summary>
  [QueryParam]
  public int PageNumber { get; init; } = 1;

  /// <summary>
  /// Number of items per page. Defaults to 10.
  /// </summary>
  [QueryParam]
  public int PageSize { get; init; } = 10;

  /// <summary>
  /// Optional search term to filter business profiles.
  /// </summary>
  [QueryParam]
  public string? SearchTerm { get; init; }
}

/// <summary>
/// API endpoint for SuperAdmins to get a paginated list of Business Profiles.
/// </summary>
public class List : Endpoint<ListAdminRequest, UseCases.Common.Models.PagedResult<BusinessProfileDTO>>
{
  private readonly ISender _sender;

  public List(ISender sender)
  {
    _sender = sender;
  }

  public override void Configure()
  {
    Get("/api/admin/businessprofiles");
    Description(d => d.AutoTagOverride("Admin_BusinessProfiles"));
    AuthSchemes(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
    Roles(SeedData.AdminRole);
    Summary(s =>
    {
      s.Summary = "List Business Profiles (Admin)";
      s.Description = "Allows a SuperAdmin to retrieve a paginated list of business profiles, with optional search.";
      s.Response<UseCases.Common.Models.PagedResult<BusinessProfileDTO>>(200, "A paginated list of business profiles.");
      s.Response(401, "Unauthorized if the user is not authenticated.");
      s.Response(403, "Forbidden if the user is not a SuperAdmin.");
    });
    Tags("Admin_BusinessProfiles");
  }

  public override async Task HandleAsync(ListAdminRequest req, CancellationToken ct)
  {
    var query = new ListBusinessProfilesAdminQuery(req.PageNumber, req.PageSize, req.SearchTerm);
    var result = await _sender.Send(query, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
    }
    else 
    {
      AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred while listing business profiles.");
      if (result.ValidationErrors.Any())
      {
        AddError(result.ValidationErrors.First().ErrorMessage);
      }
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
