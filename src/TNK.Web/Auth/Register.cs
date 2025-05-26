using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using TNK.Core.Identity; 
using TNK.Infrastructure.Data; 

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
  private readonly RoleManager<IdentityRole> _roleManager; // To validate role

  public RegisterEndpoint(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
  {
    _userManager = userManager;
    _roleManager = roleManager;
  }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous(); // Registration should be accessible without authentication
        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Registers a new user with the specified details and role.";
            s.ExampleRequest = new RegisterRequest { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Password = "Password123!", ConfirmPassword = "Password123!", Role = SeedData.CustomerRole };
            s.ResponseExamples[200] = new RegisterResponse { UserId = Guid.NewGuid().ToString(), Message = "Registration successful." };
        });

    }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    // Validate the role
    var validRoles = new[] { SeedData.VendorRole, SeedData.CustomerRole }; // Using roles from SeedData
    if (!validRoles.Contains(req.Role) || !await _roleManager.RoleExistsAsync(req.Role))
    {
      AddError(r => r.Role, $"Invalid role specified. Valid roles are: {string.Join(", ", validRoles)}");
    }

    if (ValidationFailed)
    {
      await SendErrorsAsync(400, ct);
      return;
    }

    var existingUser = await _userManager.FindByEmailAsync(req.Email);
    if (existingUser != null)
    {
      AddError(r => r.Email, "An account with this email address already exists.");
      await SendErrorsAsync(400, ct); // Or 409 Conflict
      return;
    }

    var newUser = new ApplicationUser
    {
      FirstName = req.FirstName,
      LastName = req.LastName,
      Email = req.Email,
      UserName = req.Email // Typically UserName is the same as Email
    };

    var result = await _userManager.CreateAsync(newUser, req.Password);

    if (!result.Succeeded)
    {
      foreach (var error in result.Errors)
      {
        AddError(error.Description); // Generic error
      }
      await SendErrorsAsync(400, ct);
      return;
    }

    // Add user to role
    var roleResult = await _userManager.AddToRoleAsync(newUser, req.Role);
    if (!roleResult.Succeeded)
    {
      // Log this error, potentially clean up user if role assignment is critical
      // For now, send a generic error or a specific one if preferred
      AddError($"User created but failed to assign role '{req.Role}'. Please contact support.");
      // Consider if you should delete the user here or handle it differently
      // await _userManager.DeleteAsync(newUser); // Example cleanup
      await SendErrorsAsync(500, ct);
      return;
    }

    await SendOkAsync(new RegisterResponse
    {
      UserId = newUser.Id,
      Message = "Registration successful."
    }, ct);
  }
}
