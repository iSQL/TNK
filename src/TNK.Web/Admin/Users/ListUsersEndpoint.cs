using Microsoft.AspNetCore.Authentication.JwtBearer; 
using TNK.Infrastructure.Data; 
using TNK.UseCases.Users.Admin; 

namespace TNK.Web.Admin.Users;

public class ListUsersRequest
{
  [QueryParam] public int? PageNumber { get; init; } = 1;
  [QueryParam] public int PageSize { get; init; } = 10;
  [QueryParam] public string? SearchTerm { get; init; }
}

public class ListUsersEndpoint : Endpoint<ListUsersRequest, Result<UseCases.Common.Models.PagedResult<UserDetailsAdminDTO>>> 
{
  private readonly ISender _sender;

  public ListUsersEndpoint(ISender sender) => _sender = sender;

  public override void Configure()
  {
    Get("/api/admin/users");
    Description(d => d.AutoTagOverride("Admin_Users"));

    AuthSchemes(JwtBearerDefaults.AuthenticationScheme); 
    Roles(Core.Constants.Roles.Admin); 
    
    Summary(s => {
      s.ExampleRequest = new ListUsersRequest
      {
        PageNumber = 1,
        PageSize = 10        
      };
      s.Summary = "List all users (Admin)";
      s.Description = "Allows a SuperAdmin to retrieve a paginated list of all users and their roles.";
      s.Responses[200] = "Successfully retrieved the list of users.";
      s.Responses[401] = "Unauthorized.";
      s.Responses[403] = "Forbidden.";
    });
  }

  public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
  {
    var query = new ListUsersAdminQuery(req.PageNumber, req.PageSize, req.SearchTerm);
    var result = await _sender.Send(query, ct);

    if (result.IsSuccess)
    {
      await SendOkAsync(result.Value, ct); 
    }
    else
    {
      AddError(result.Errors.FirstOrDefault() ?? "Failed to retrieve users.");
      await SendErrorsAsync(cancellation: ct);
    }
  }
}
