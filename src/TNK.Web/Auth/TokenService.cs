using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TNK.Core.Identity;

namespace TNK.Web.Auth;

public class TokenService
{
  private readonly IConfiguration _configuration;
  private readonly UserManager<ApplicationUser> _userManager;

  public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
  {
    _configuration = configuration;
    _userManager = userManager;
  }

  public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
  {
    var jwtSettings = _configuration.GetSection("JwtSettings");
    var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

    var userRoles = await _userManager.GetRolesAsync(user);

    var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id), 
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
            new Claim(ClaimTypes.NameIdentifier, user.Id), 
            new Claim(ClaimTypes.Name, user.UserName!), 
            // Add custom claims like FirstName, LastName if needed directly in the token
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("lastName", user.LastName ?? string.Empty)
        };

    foreach (var userRole in userRoles)
    {
      claims.Add(new Claim(ClaimTypes.Role, userRole));
    }

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(claims),
      Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"] ?? "60")),
      Issuer = jwtSettings["Issuer"],
      Audience = jwtSettings["Audience"],
      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
  }
}
