using Microsoft.AspNetCore.Mvc;
using TNK.Infrastructure.Data; 
using TNK.UseCases.BusinessProfiles.DeleteAdmin;

namespace TNK.Web.Admin.BusinessProfiles;

/// <summary>
/// Request DTO for deleting a Business Profile by ID (Admin).
/// The ID is taken from the route.
/// </summary>
public record DeleteAdminBusinessProfileRequest
{
  /// <summary>
  /// The ID of the Business Profile to delete.
  /// </summary>
  [FromRoute]
  public int BusinessProfileId { get; init; }
}

/// <summary>
/// API endpoint for SuperAdmins to delete an existing Business Profile.
/// </summary>
public class Delete : Endpoint<DeleteAdminBusinessProfileRequest> 
{
  private readonly ISender _sender;

  public Delete(ISender sender)
  {
    _sender = sender;
  }

  public override void Configure()
  {
    Delete("/api/admin/businessprofiles/{BusinessProfileId}");
    Description(d => d.AutoTagOverride("Admin_BusinessProfiles"));
    AuthSchemes(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
    Roles(SeedData.AdminRole);
    Summary(s =>
    {
      s.Summary = "Delete an existing Business Profile (Admin)";
      s.Description = "Allows a SuperAdmin to delete an existing business profile by its ID.";
      s.Response(204, "Business profile deleted successfully.");
      s.Response(401, "Unauthorized if the user is not authenticated.");
      s.Response(403, "Forbidden if the user is not a SuperAdmin.");
      s.Response(404, "Not Found if the business profile does not exist.");
    });
    Tags("Admin_BusinessProfiles");
  }

  public override async Task HandleAsync(DeleteAdminBusinessProfileRequest req, CancellationToken ct)
  {
    if (req.BusinessProfileId <= 0)
    {
      AddError("BusinessProfileId in route must be a positive integer.");
      await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
      return;
    }

    var command = new AdminDeleteBusinessProfileCommand(req.BusinessProfileId);
    var result = await _sender.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct); // 204 No Content for successful DELETE
      return;
    }

    switch (result.Status)
    {
      case ResultStatus.NotFound:
        await SendNotFoundAsync(ct);
        return;

      default: 
        AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred while deleting the business profile.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        return;
    }
  }
}
