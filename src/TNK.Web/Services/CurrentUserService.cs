// File: TNK.Web/Services/CurrentUserService.cs (or a suitable infrastructure location)
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TNK.Core.Interfaces; // Reference to your ICurrentUserService

namespace TNK.Web.Services; // Adjust namespace as per your project structure

public class CurrentUserService : ICurrentUserService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

  public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

  public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

  public int? BusinessProfileId
  {
    get
    {
      var businessProfileIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("business_profile_id");
      if (int.TryParse(businessProfileIdClaim, out var businessProfileId))
      {
        return businessProfileId;
      }
      return null;
    }
  }

  public IEnumerable<string> Roles
  {
    get
    {
      return _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
                                  .Select(c => c.Value) ?? Enumerable.Empty<string>();
    }
  }

  public bool IsInRole(string roleName)
  {
    return _httpContextAccessor.HttpContext?.User?.IsInRole(roleName) ?? false;
  }
}
