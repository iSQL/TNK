using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using TNK.Core.Identity;
using TNK.Infrastructure.Data;
using TNK.Web.Resources;
using TNK.Core.Constants;

namespace TNK.Web.Auth;

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

public class RegisterResponse
{
  public string UserId { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
}

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly IStringLocalizer<SharedResources> _localizer; 


  public RegisterEndpoint(
      UserManager<ApplicationUser> userManager,
      RoleManager<IdentityRole> roleManager,
      IStringLocalizer<SharedResources> localizer)
  {
    _userManager = userManager;
    _roleManager = roleManager;
    _localizer = localizer;
  }

  public override void Configure()
  {
    Post("/api/auth/register");
    Description(d => d.AutoTagOverride("Auth"));

    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Register a new user";
      s.Description = "Registers a new user with the specified details and role.";
      s.ExampleRequest = new RegisterRequest { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Password = "Password123!", ConfirmPassword = "Password123!", Role = Core.Constants.Roles.Admin };
    });
  }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    string[] validRoles = new[] { Core.Constants.Roles.Admin, Core.Constants.Roles.Customer, Core.Constants.Roles.Admin };
    if (! validRoles.Contains(req.Role) || !await _roleManager.RoleExistsAsync(req.Role))
    {
      // Use localized string with parameter
      AddError(r => r.Role, _localizer["InvalidRole", string.Join(", ", validRoles)]);
    }

    if (ValidationFailed)
    {
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct);
      return;
    }

    var existingUser = await _userManager.FindByEmailAsync(req.Email);
    if (existingUser != null)
    {
      AddError(r => r.Email, _localizer["UserAlreadyExists"]); 
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct); 
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
          localizedErrorMessage = !string.IsNullOrEmpty(error.Description) ? error.Description : _localizer["DefaultIdentityError"];
        }
        AddError(localizedErrorMessage);
      }
      await SendErrorsAsync(statusCode: StatusCodes.Status400BadRequest, cancellation: ct);
      return;
    }

    var roleResult = await _userManager.AddToRoleAsync(newUser, req.Role);
    if (!roleResult.Succeeded)
    {
      AddError(_localizer["RoleAssignmentFailed", req.Role]); 
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
