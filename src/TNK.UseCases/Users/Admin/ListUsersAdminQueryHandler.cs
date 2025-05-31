using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TNK.Core.Identity;

namespace TNK.UseCases.Users.Admin;

public class ListUsersAdminQueryHandler : IRequestHandler<ListUsersAdminQuery, Result<Common.Models.PagedResult<UserDetailsAdminDTO>>>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly ILogger<ListUsersAdminQueryHandler> _logger;

  public ListUsersAdminQueryHandler(UserManager<ApplicationUser> userManager, ILogger<ListUsersAdminQueryHandler> logger)
  {
    _userManager = userManager;
    _logger = logger;
  }

  public async Task<Result<Common.Models.PagedResult<UserDetailsAdminDTO>>> Handle(ListUsersAdminQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var queryableUsers = _userManager.Users;

      if (!string.IsNullOrWhiteSpace(request.SearchTerm))
      {
        var term = request.SearchTerm.ToLowerInvariant();
        queryableUsers = queryableUsers.Where(u =>
            (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
            (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
            (u.Email != null && u.Email.ToLower().Contains(term)) ||
            (u.UserName != null && u.UserName.ToLower().Contains(term))
        );
      }

      var totalCount = await queryableUsers.CountAsync(cancellationToken);

      var users = await queryableUsers
          .OrderBy(u => u.LastName) // Or by Email / UserName
          .ThenBy(u => u.FirstName)
          .Skip(((request.PageNumber ?? 1) - 1) * request.PageSize)
          .Take(request.PageSize)
          .ToListAsync(cancellationToken);

      var userDtos = new List<UserDetailsAdminDTO>();
      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);
        userDtos.Add(new UserDetailsAdminDTO(
            user.Id,
            user.FirstName ?? "", 
            user.LastName ?? "",  
            user.Email ?? "",
            user.PhoneNumber,
            user.EmailConfirmed,
            roles.ToList()
        ));
      }

      var pagedResult = new Common.Models.PagedResult<UserDetailsAdminDTO>(userDtos, totalCount, request.PageNumber ?? 1, request.PageSize);
      return Result.Success(pagedResult);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching users for admin in ListUsersAdminQueryHandler.");
      return Result.Error("An error occurred while fetching users."); // Consider more specific error messages or codes
    }
  }
}
