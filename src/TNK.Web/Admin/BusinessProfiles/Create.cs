using Ardalis.Result;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
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
[Tags("Admin_BusinessProfiles")]
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
    Tags("Admin_BusinessProfiles");
    AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
    Roles(SeedData.AdminRole);
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
      // For POST, typically return 201 Created with a Location header and the created resource.
      // The GetById endpoint for admin business profiles should have a name for RouteName.
      // Let's assume the GetById endpoint (TNK.Web.Admin.BusinessProfiles.GetById) has a route name.
      // If not, you might need to add one or adjust this.
      // For simplicity here, we'll send 201 with the DTO.
      // A more complete implementation would use SendCreatedAtAsync:
      // await SendCreatedAtAsync<GetById>(new { BusinessProfileId = result.Value.Id }, result.Value, generateAbsoluteUrl: true, cancellation: ct);
      // This requires the GetById endpoint to have a .WithName("GetBusinessProfileByIdAdmin") or similar.

      await SendCreatedAtAsync<TNK.Web.Admin.BusinessProfiles.GetById>( // Manually construct route for simplicity if GetById has no name
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
        // Add validation errors to the response
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
      default: // Includes ResultStatus.Error
        AddError(result.Errors.FirstOrDefault() ?? "An unexpected error occurred while creating the business profile.");
        await SendErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        return;
    }
  }
}
