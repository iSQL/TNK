using System.ComponentModel.DataAnnotations;
using System.Linq; // For FirstOrDefault on Errors
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result; // For ValidationError
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Identity;
using TNK.Core.Identity; // Your ApplicationUser
using TNK.Infrastructure.Data; // For SeedData.VendorRole
using TNK.UseCases.BusinessProfile.Create;
using TNK.UseCases.BusinessProfiles.Create; // Your CreateBusinessProfileCommand

namespace TNK.Web.BusinessProfiles.Create; // Ensure this namespace matches your folder structure

// 1. Request DTO
public class CreateBusinessProfileRequest
{
  [Required(ErrorMessage = "Business name is required.")]
  [MaxLength(200, ErrorMessage = "Business name cannot exceed 200 characters.")]
  public string Name { get; set; } = string.Empty;

  [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
  public string? Address { get; set; }

  [MaxLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
  [Phone(ErrorMessage = "Invalid phone number format.")]
  public string? PhoneNumber { get; set; }

  [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
  public string? Description { get; set; }
}

// 2. Response DTO
public class CreateBusinessProfileResponse
{
  public int ProfileId { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
}

// 3. Endpoint Class - Ensure this class definition exists and is public
public class Endpoint : Endpoint<CreateBusinessProfileRequest, CreateBusinessProfileResponse>
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
    Post("/api/businessprofiles"); // This is the route for POST
    Roles(SeedData.VendorRole);
    Summary(s =>
    {
      s.Summary = "Create a new business profile";
      s.Description = "Allows an authenticated vendor to create their business profile.";
      s.ExampleRequest = new CreateBusinessProfileRequest { Name = "Novi vendor", Address ="Adresa bb", Description ="Bez opisa", PhoneNumber = "" };
    });
    Options(x => x.WithName("CreateBusinessProfile"));
    // Add other configurations like Description for Produces/Accepts if needed
  }

  public override async Task HandleAsync(CreateBusinessProfileRequest req, CancellationToken ct)
  {
    var vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(vendorId))
    {
      _logger.LogWarning("VendorId not found in claims for business profile creation.");
      await SendUnauthorizedAsync(ct);
      return;
    }

    var command = new CreateBusinessProfileCommand(
        vendorId,
        req.Name,
        req.Address,
        req.PhoneNumber,
        req.Description
    );

    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      _logger.LogWarning("Failed to create business profile for VendorId {VendorId}. Status: {Status}, Errors: {Errors}, ValidationErrors: {ValidationErrors}",
          vendorId,
          result.Status,
          string.Join("; ", result.Errors),
          ArdalisResultExtensions.ValidationErrorsString(result)); // Call as static extension

      switch (result.Status)
      {
        case Ardalis.Result.ResultStatus.Invalid:
          foreach (var error in result.ValidationErrors)
          {
            AddError(error.Identifier, error.ErrorMessage);
          }
          await SendErrorsAsync(400, ct);
          return;
        case Ardalis.Result.ResultStatus.Conflict:
          await SendProblemDetailsAsync(
              title: "Conflict",
              instance: HttpContext.Request.Path,
              statusCode: 409,
              detail: result.Errors.FirstOrDefault() ?? "A business profile for this vendor likely already exists.",
              cancellation: ct
          );
          return;
        case Ardalis.Result.ResultStatus.NotFound:
          await SendNotFoundAsync(ct);
          return;
        default:
          await SendProblemDetailsAsync(
              title: "Internal Server Error",
              instance: HttpContext.Request.Path,
              statusCode: 500,
              detail: result.Errors.FirstOrDefault() ?? "An unexpected error occurred while creating the business profile.",
              cancellation: ct
          );
          return;
      }
    }

    var responseBody = new CreateBusinessProfileResponse
    {
      ProfileId = result.Value,
      Name = req.Name,
      Message = "Business profile created successfully."
    };
    await SendAsync(responseBody, 201, ct);
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

 public static class ArdalisResultExtensions
{
  public static string ValidationErrorsString(this Ardalis.Result.IResult resultStatus)
  {
    if (resultStatus == null || resultStatus.ValidationErrors == null || !resultStatus.ValidationErrors.Any())
      return string.Empty;
    return string.Join("; ", resultStatus.ValidationErrors.Select(e => $"{e.Identifier}: {e.ErrorMessage}"));
  }
}
