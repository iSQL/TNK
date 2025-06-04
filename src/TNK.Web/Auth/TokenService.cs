// Suggested Location: TNK.Web/Auth/TokenService.cs (or wherever it currently resides)
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // For AsNoTracking, FirstOrDefaultAsync
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TNK.Core.Identity;        // For ApplicationUser
using TNK.Infrastructure.Data;  // For AppDbContext (ensure this using is correct)
// using TNK.Core.Constants;    // If you need Roles constants here

namespace TNK.Web.Auth; // Or your appropriate namespace

public class TokenService
{
  private readonly IConfiguration _configuration;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly AppDbContext _dbContext; // Inject AppDbContext
  private readonly ILogger<TokenService> _logger;

  public TokenService(
      IConfiguration configuration,
      UserManager<ApplicationUser> userManager,
      AppDbContext dbContext, // Add AppDbContext
      ILogger<TokenService> logger)
  {
    _configuration = configuration;
    _userManager = userManager;
    _dbContext = dbContext; // Store injected AppDbContext
    _logger = logger;
  }

  public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
  {
    if (user == null)
    {
      throw new ArgumentNullException(nameof(user));
    }

    _logger.LogInformation("[TokenService] Generating JWT for User: {UserName}, ID: {UserId}", user.UserName, user.Id);

    var claims = new List<Claim>
          {
              new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Subject (user ID)
              new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID unique for each token
              new Claim(ClaimTypes.NameIdentifier, user.Id), // Standard claim for User ID
              new Claim(ClaimTypes.Name, user.UserName ?? string.Empty), // Standard claim for UserName
          };

    // Add FirstName and LastName if they exist
    if (!string.IsNullOrEmpty(user.FirstName))
    {
      claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
    }
    if (!string.IsNullOrEmpty(user.LastName))
    {
      claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
    }

    // Add roles as claims
    var roles = await _userManager.GetRolesAsync(user);
    foreach (var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
      _logger.LogInformation("[TokenService] Added Role Claim: {Role} for User: {UserName}", role, user.UserName);
    }

    // Add BusinessProfileId claim if the user is a Vendor
    if (roles.Contains(Core.Constants.Roles.Vendor)) // Assuming Roles.Vendor is "Vendor"
    {
      try
      {
        var businessProfile = await _dbContext.BusinessProfiles
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(bp => bp.VendorId == user.Id);
        if (businessProfile != null)
        {
          claims.Add(new Claim("business_profile_id", businessProfile.Id.ToString()));
          _logger.LogInformation("[TokenService] Added 'business_profile_id': {BusinessProfileId} to JWT for Vendor User: {UserName}", businessProfile.Id, user.UserName);
        }
        else
        {
          _logger.LogWarning("[TokenService] No BusinessProfile found for Vendor User ID: {UserId}. 'business_profile_id' claim NOT added to JWT.", user.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "[TokenService] Exception while fetching BusinessProfile for User ID: {UserId} during JWT generation.", user.Id);
      }
    }

    var jwtSettings = _configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];
    if (string.IsNullOrEmpty(secretKey))
    {
      _logger.LogError("[TokenService] JWT SecretKey is null or empty in configuration.");
      throw new InvalidOperationException("JWT SecretKey is not configured.");
    }
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var durationInMinutesString = jwtSettings["DurationInMinutes"];
    if (!double.TryParse(durationInMinutesString, out var durationInMinutes))
    {
      durationInMinutes = 360; // Default duration if parsing fails or not configured
      _logger.LogWarning("[TokenService] JWT DurationInMinutes not configured or invalid, defaulting to {DefaultDuration} minutes.", durationInMinutes);
    }
    var expires = DateTime.UtcNow.AddMinutes(durationInMinutes);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(claims),
      Expires = expires,
      Issuer = jwtSettings["Issuer"],
      Audience = jwtSettings["Audience"],
      SigningCredentials = creds
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);

    _logger.LogInformation("[TokenService] JWT successfully generated for User: {UserName}", user.UserName);
    return tokenHandler.WriteToken(token);
  }
}
