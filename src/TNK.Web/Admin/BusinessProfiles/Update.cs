
using Microsoft.AspNetCore.Mvc;
using TNK.Core.BusinessAggregate;
using TNK.Infrastructure.Data; 
using TNK.UseCases.BusinessProfiles; 
using TNK.UseCases.BusinessProfiles.UpdateAdmin; 

namespace TNK.Web.Admin.BusinessProfiles;

/// <summary>
/// Request DTO for updating a Business Profile by an Admin.
/// The BusinessProfileId is taken from the route.
/// </summary>
public record UpdateAdminBusinessProfileRequest
{
  [FromRoute] 
  public int BusinessProfileId { get; init; } // This will be a route parameter


  /// <summary>
  /// The new name of the business.
  /// </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// The new address of the business.
  /// </summary>
  public string? Address { get; init; }

  /// <summary>
  /// The new phone number of the business.
  /// </summary>
  public string? PhoneNumber { get; init; }

  /// <summary>
  /// The new description of the business.
  /// </summary>
  public string? Description { get; init; }
}

/// <summary>
/// API endpoint for SuperAdmins to update an existing Business Profile.
/// </summary>
public class Update : Endpoint<UpdateAdminBusinessProfileRequest, BusinessProfileDTO>
{
  private readonly ISender _sender;

  public Update(ISender sender)
  {
    _sender = sender;
  }

  public override void Configure()
  {
    Put("/api/admin/businessprofiles/{BusinessProfileId}");
    AuthSchemes(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
    Roles(SeedData.AdminRole);
    Summary(s =>
    {
      s.Summary = "Update an existing Business Profile (Admin)";
      s.Description = "Allows a SuperAdmin to update the details of an existing business profile.";
      s.ExampleRequest = new UpdateAdminBusinessProfileRequest
      {
        Name = "Updated Super Services Ltd.",
        Address = "456 New Ave, Anytown",
        PhoneNumber = "555-5678",
        Description = "Now offering even more super services."
      };
      s.Response<BusinessProfileDTO>(200, "Business profile updated successfully.");
      // s.Response(204, "Business profile updated successfully (No Content)."); // Alternative success response
      s.Response(400, "Invalid request parameters.");
      s.Response(401, "Unauthorized if the user is not authenticated.");
      s.Response(403, "Forbidden if the user is not a SuperAdmin.");
      s.Response(404, "Not Found if the business profile does not exist.");
    });
    Tags("Admin_BusinessProfiles");
  }

  public override async Task HandleAsync(UpdateAdminBusinessProfileRequest req, CancellationToken ct)
  {
    // BusinessProfileId is bound from the route by FastEndpoints automatically
    // if a property with the same name exists in the request DTO or the endpoint class itself.
    // To make it explicit and clean, we can access it via Route<int>("BusinessProfileId")
    if (req.BusinessProfileId <= 0 )
    {
      AddError("BusinessProfileId in route must be a positive integer.");
      await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
      return;
    }

    var command = new AdminUpdateBusinessProfileCommand(
        req.BusinessProfileId,
        req.Name,
        req.Address,
        req.PhoneNumber,
        req.Description
    );

    var result = await _sender.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct);
      // Alternatively, for PUT operations that update, a 204 No Content is also common:
      // await SendNoContentAsync(ct);
      return;
    }

    switch (result.Status)
    {
      case ResultStatus.NotFound:
        await SendNotFoundAsync(ct);
        return;
      case ResultStatus.Invalid:
        foreach (var error in result.ValidationErrors)
        {
          AddError(error.ErrorMessage, error.Identifier);
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        return;
      default: // Includes ResultStatus.Error
        AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred while updating the business profile.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        return;
    }
  }
}
