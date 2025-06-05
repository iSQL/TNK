// Suggested Location: TNK.Web/Auth/TokenService.cs (or wherever it currently resides)
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TNK.Core.Identity;
using TNK.Infrastructure.Data;  // For AppDbContext
using Microsoft.Extensions.Logging; // For ILogger

namespace TNK.Web.Auth;

public class TokenService
{
  private readonly IConfiguration _configuration;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly AppDbContext _dbContext;
  private readonly ILogger<TokenService> _logger;

  public TokenService(
      IConfiguration configuration,
      UserManager<ApplicationUser> userManager,
      AppDbContext dbContext,
      ILogger<TokenService> logger)
  {
    _configuration = configuration;
    _userManager = userManager;
    _dbContext = dbContext;
    _logger = logger;
  }

  public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
  {
    if (user == null)
    {
      _logger.LogError("[TokenService] Attempted to generate JWT for a null user.");
      throw new ArgumentNullException(nameof(user));
    }

    _logger.LogInformation("[TokenService] Generating JWT for User: {UserName}, ID: {UserId}", user.UserName, user.Id);

    var claims = new List<Claim>
          {
              new Claim(JwtRegisteredClaimNames.Sub, user.Id),
              new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
              new Claim(ClaimTypes.NameIdentifier, user.Id),
              new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
          };

    if (!string.IsNullOrEmpty(user.FirstName))
    {
      claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
      _logger.LogInformation("[TokenService] Added GivenName Claim: {FirstName} for User: {UserName}", user.FirstName, user.UserName);
    }
    if (!string.IsNullOrEmpty(user.LastName))
    {
      claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
      _logger.LogInformation("[TokenService] Added Surname Claim: {LastName} for User: {UserName}", user.LastName, user.UserName);
    }

    var roles = await _userManager.GetRolesAsync(user);
    foreach (var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
      _logger.LogInformation("[TokenService] Added Role Claim: {Role} for User: {UserName}", role, user.UserName);
    }

    // Add BusinessProfileId claim if the user is a Vendor
    if (roles.Contains(Core.Constants.Roles.Vendor))
    {
      try
      {
        var businessProfile = await _dbContext.BusinessProfiles
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(bp => bp.VendorId == user.Id);
        if (businessProfile != null)
        {
          claims.Add(new Claim("BusinessProfileId", businessProfile.Id.ToString()));
          _logger.LogInformation("[TokenService] Added 'BusinessProfileId': {BusinessProfileId} to JWT for Vendor User: {UserName}", businessProfile.Id, user.UserName);
        }
        else
        {
          _logger.LogWarning("[TokenService] No BusinessProfile found for Vendor User ID: {UserId}. 'BusinessProfileId' claim NOT added to JWT.", user.Id);
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
      _logger.LogCritical("[TokenService] JWT SecretKey is null or empty in configuration. Cannot generate token.");
      throw new InvalidOperationException("JWT SecretKey is not configured.");
    }
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var durationInMinutesString = jwtSettings["DurationInMinutes"];
    if (!double.TryParse(durationInMinutesString, out var durationInMinutes))
    {
      durationInMinutes = 360;
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
    var securityToken = tokenHandler.CreateToken(tokenDescriptor); // Changed variable name to avoid conflict

    _logger.LogInformation("[TokenService] JWT successfully generated for User: {UserName}, expiring at {Expiration}", user.UserName, expires);
    return tokenHandler.WriteToken(securityToken);
  }
}
