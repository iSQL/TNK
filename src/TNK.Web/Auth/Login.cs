using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using TNK.Core.Identity;
using TNK.Web.Resources;

namespace TNK.Web.Auth;

public class LoginRequest
{
  [Required, EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string Password { get; set; } = string.Empty;
}

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
  private readonly TokenService _tokenService;
  private readonly IConfiguration _configuration;
  private readonly IStringLocalizer<SharedResources> _localizer;

  public LoginEndpoint(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      TokenService tokenService,
      IConfiguration configuration,
      IStringLocalizer<SharedResources> localizer)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _tokenService = tokenService;
    _configuration = configuration;
    _localizer = localizer;
  }

  public override void Configure()
  {
    Post("/api/auth/login");
    Description(d => d.AutoTagOverride("Auth"));

    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "User login";
      s.Description = "Authenticates a user and returns a JWT token upon success.";
      s.ExampleRequest = new LoginRequest { Email = "admin@local", Password = "123qwe" };
    });
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    Logger.LogWarning(_localizer["InvalidLoginAttempt"]);
    Logger.LogInformation("Current UI Culture: {Culture}", System.Threading.Thread.CurrentThread.CurrentUICulture.Name);


    var user = await _userManager.FindByEmailAsync(req.Email);
    if (user == null)
    {
      Logger.LogWarning("Login failed: User {Email} not found.", req.Email);
      AddError(_localizer["InvalidLoginAttempt"]);
      await SendErrorsAsync(statusCode: StatusCodes.Status401Unauthorized, cancellation: ct);
      return;
    }

    var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);

    if (!result.Succeeded)
    {
      Logger.LogWarning("Login failed for user {Email}. Reason: {Reason}", req.Email, result.ToString());
      if (result.IsLockedOut)
      {
        AddError(_localizer["AccountLockedOut"]);
      }
      else
      {
        AddError(_localizer["InvalidLoginAttempt"]);
      }
      await SendErrorsAsync(statusCode: StatusCodes.Status401Unauthorized, cancellation: ct);
      return;
    }

    var tokenString = await _tokenService.GenerateJwtTokenAsync(user);
    var userRoles = await _userManager.GetRolesAsync(user);
    var tokenDurationMinutes = Convert.ToDouble(_configuration["JwtSettings:DurationInMinutes"] ?? "360"); //ToDo: Use a sensible default or configuration value

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
