namespace TNK.Core.Interfaces;

public interface ICurrentUserService
{
  string? UserId { get; }
  int? BusinessProfileId { get; } 
  bool IsAuthenticated { get; }
  string? Email { get; }
  IEnumerable<string> Roles { get; }
  bool IsInRole(string roleName);
}
