using Microsoft.AspNetCore.Authentication.JwtBearer;
using TNK.Infrastructure.Data;
using TNK.UseCases.BusinessProfiles;
using TNK.UseCases.BusinessProfiles.CreateAdmin;

namespace TNK.Web.Admin.BusinessProfiles;

/// <summary>
/// Request DTO for creating a Business Profile by an Admin.
/// </summary>
public record CreateAdminBusinessProfileRequest
{
  /// <summary>
  /// The ID of the ApplicationUser (vendor) for whom the profile is being created.
  /// </summary>
  public string VendorId { get; init; } = string.Empty;

  /// <summary>
  /// The name of the business.
  /// </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// The address of the business.
  /// </summary>
  public string? Address { get; init; }

  /// <summary>
  /// The phone number of the business.
  /// </summary>
  public string? PhoneNumber { get; init; }

  /// <summary>
  /// A description of the business.
  /// </summary>
  public string? Description { get; init; }
}

/// <summary>
/// API endpoint for SuperAdmins to create a new Business Profile for a specified Vendor.
/// </summary>
/// 
public class Create : Endpoint<CreateAdminBusinessProfileRequest, BusinessProfileDTO>
{
  private readonly ISender _sender;

  public Create(ISender sender)
  {
    _sender = sender;
  }

  public override void Configure()
  {
    Post("/api/admin/businessprofiles");
    Description(d => d.AutoTagOverride("Admin_BusinessProfiles"));
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Roles(Core.Constants.Roles.Admin);
    Summary(s =>
    {
      s.Summary = "Create a new Business Profile (Admin)";
      s.Description = "Allows a SuperAdmin to create a new business profile for a specified vendor.";
      s.ExampleRequest = new CreateAdminBusinessProfileRequest
      {
        VendorId = "user-guid-placeholder",
        Name = "Super Cool Services Inc.",
        Address = "123 Main St, Anytown",
        PhoneNumber = "555-1234",
        Description = "Providing the best services in town."
      };
      s.Response<BusinessProfileDTO>(201, "Business profile created successfully.");
      s.Response(400, "Invalid request parameters.");
      s.Response(401, "Unauthorized if the user is not authenticated.");
      s.Response(403, "Forbidden if the user is not a SuperAdmin.");
      s.Response(409, "Conflict if a business profile already exists for the vendor.");
    });
  }

  public override async Task HandleAsync(CreateAdminBusinessProfileRequest req, CancellationToken ct)
  {
    var command = new AdminCreateBusinessProfileCommand(
        req.VendorId,
        req.Name,
        req.Address,
        req.PhoneNumber,
        req.Description
    );

    var result = await _sender.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendCreatedAtAsync<GetById>( 
                routeValues: new { BusinessProfileId = result.Value.Id },
                responseBody: result.Value,
                generateAbsoluteUrl: true,
                cancellation: ct
            );

      return;
    }

    switch (result.Status)
    {
      case ResultStatus.Invalid:
        foreach (var error in result.ValidationErrors)
        {
          AddError(error.ErrorMessage, error.Identifier);
        }
        await SendErrorsAsync(StatusCodes.Status400BadRequest, ct);
        return;
      case ResultStatus.Conflict:
        AddError(result.Errors.FirstOrDefault() ?? "A business profile already exists for this vendor.");
        await SendErrorsAsync(StatusCodes.Status409Conflict, ct);
        return;
      default: 
        AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred while creating the business profile.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        return;
    }
  }
}
