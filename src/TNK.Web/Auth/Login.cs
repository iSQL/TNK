using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using TNK.Core.Identity; // Your ApplicationUser

namespace TNK.Web.Auth;

// Request DTO
public class LoginRequest
{
  [Required, EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string Password { get; set; } = string.Empty;
}

// Response DTO
public class LoginResponse
{
  public string Token { get; set; } = string.Empty;
  public DateTime Expiration { get; set; }
  public string UserId { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public List<string> Roles { get; set; } = new();
}

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly SignInManager<ApplicationUser> _signInManager;
  private readonly TokenService _tokenService; // Inject the token service
  private readonly IConfiguration _configuration; // For JWT Expiration

  public LoginEndpoint(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      TokenService tokenService,
      IConfiguration configuration)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _tokenService = tokenService;
    _configuration = configuration;
  }

  public override void Configure()
  {
    Post("/api/auth/login");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "User login";
      s.Description = "Authenticates a user and returns a JWT token upon success.";
      s.ExampleRequest = new LoginRequest { Email = "john.doe@example.com", Password = "Password123!" };
      // Response example can be more detailed if desired
    });
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    var user = await _userManager.FindByEmailAsync(req.Email);
    if (user == null)
    {
      Logger.LogWarning("Login failed: User {Email} not found.", req.Email);
      AddError("Invalid email or password."); // Generic message for security
      await SendUnauthorizedAsync(ct); // Or SendErrorsAsync(400) or specific 401
      return;
    }

    // Check if email is confirmed if you have that feature enabled
    // if (!await _userManager.IsEmailConfirmedAsync(user))
    // {
    //     AddError("Email not confirmed.");
    //     await SendUnauthorizedAsync(ct);
    //     return;
    // }

    var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);

    if (!result.Succeeded)
    {
      Logger.LogWarning("Login failed for user {Email}. Reason: {Reason}", req.Email, result.ToString());
      if (result.IsLockedOut)
      {
        AddError("Account locked out due to too many failed login attempts.");
      }
      // else if (result.IsNotAllowed) // For things like email not confirmed
      // {
      //     AddError("Login not allowed. Please confirm your email or contact support.");
      // }
      else
      {
        AddError("Invalid email or password.");
      }
      await SendUnauthorizedAsync(ct);
      return;
    }

    var tokenString = await _tokenService.GenerateJwtTokenAsync(user);
    var userRoles = await _userManager.GetRolesAsync(user);
    var tokenDurationMinutes = Convert.ToDouble(_configuration["JwtSettings:DurationInMinutes"] ?? "60");

    await SendOkAsync(new LoginResponse
    {
      Token = tokenString,
      Expiration = DateTime.UtcNow.AddMinutes(tokenDurationMinutes),
      UserId = user.Id,
      Email = user.Email!,
      FirstName = user.FirstName ?? string.Empty,
      LastName = user.LastName ?? string.Empty,
      Roles = userRoles.ToList()
    }, ct);
  }
}
