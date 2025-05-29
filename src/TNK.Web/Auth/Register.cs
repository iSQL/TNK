using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using TNK.Core.Identity;
using TNK.Infrastructure.Data;
using Microsoft.Extensions.Localization; 
using TNK.Web.Resources; 

namespace TNK.Web.Auth;

// Request DTO
public class RegisterRequest
{
  [Required, MaxLength(100)]
  public string FirstName { get; set; } = string.Empty;

  [Required, MaxLength(100)]
  public string LastName { get; set; } = string.Empty;

  [Required, EmailAddress, MaxLength(256)]
  public string Email { get; set; } = string.Empty;

  [Required, MinLength(8), MaxLength(100)]
  public string Password { get; set; } = string.Empty;

  [Required, Compare(nameof(Password))]
  public string ConfirmPassword { get; set; } = string.Empty;

  [Required]
  public string Role { get; set; } = string.Empty; // "Vendor" or "Customer"
}

// Response DTO
public class RegisterResponse
{
  public string UserId { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
}

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly IStringLocalizer<SharedResources> _localizer; // Inject IStringLocalizer


  public RegisterEndpoint(
      UserManager<ApplicationUser> userManager,
      RoleManager<IdentityRole> roleManager,
      IStringLocalizer<SharedResources> localizer) // Add to constructor
  {
    _userManager = userManager;
    _roleManager = roleManager;
    _localizer = localizer; // Assign
  }

  public override void Configure()
  {
    Post("/api/auth/register");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Register a new user";
      s.Description = "Registers a new user with the specified details and role.";
      s.ExampleRequest = new RegisterRequest { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Password = "Password123!", ConfirmPassword = "Password123!", Role = SeedData.CustomerRole };
      // Update example response to use localizer if the message is static,
      // or construct it dynamically in HandleAsync if it changes.
      // For now, we'll localize the message in HandleAsync.
    });
  }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    var validRoles = new[] { SeedData.VendorRole, SeedData.CustomerRole, SeedData.AdminRole };
    if (!validRoles.Contains(req.Role) || !await _roleManager.RoleExistsAsync(req.Role))
    {
      // Use localized string with parameter
      AddError(r => r.Role, _localizer["InvalidRole", string.Join(", ", validRoles)]);
    }

    if (ValidationFailed)
    {
      // SendErrorsAsync automatically sends ValidationFailures with a 400
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct);
      return;
    }

    var existingUser = await _userManager.FindByEmailAsync(req.Email);
    if (existingUser != null)
    {
      AddError(r => r.Email, _localizer["UserAlreadyExists"]); // Use localized string
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct); // Or StatusCodes.Status409Conflict
      return;
    }

    var newUser = new ApplicationUser
    {
      FirstName = req.FirstName,
      LastName = req.LastName,
      Email = req.Email,
      UserName = req.Email
    };

    var result = await _userManager.CreateAsync(newUser, req.Password);

    if (!result.Succeeded)
    {
      foreach (var error in result.Errors)
      {
        // Attempt to localize Identity error codes. If a specific key for error.Code exists, use it.
        // Otherwise, fall back to error.Description or a generic localized message.
        var localizedErrorMessage = _localizer[error.Code]?.Value;
        if (string.IsNullOrEmpty(localizedErrorMessage) || localizedErrorMessage == error.Code)
        {
          // If specific localization for error.Code is not found, use error.Description (which is Identity's default English message)
          // Or, provide a more generic localized fallback.
          localizedErrorMessage = !string.IsNullOrEmpty(error.Description) ? error.Description : _localizer["DefaultIdentityError"];
        }
        AddError(localizedErrorMessage); // Add the (potentially localized) error description
      }
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct);
      return;
    }

    var roleResult = await _userManager.AddToRoleAsync(newUser, req.Role);
    if (!roleResult.Succeeded)
    {
      AddError(_localizer["RoleAssignmentFailed", req.Role]); // Use localized string with parameter
      await SendErrorsAsync(statusCode: StatusCodes.Status500InternalServerError, cancellation: ct);
      return;
    }

    await SendOkAsync(new RegisterResponse
    {
      UserId = newUser.Id,
      Message = _localizer["UserRegistrationSuccessful"] 
    }, ct);
  }
}
