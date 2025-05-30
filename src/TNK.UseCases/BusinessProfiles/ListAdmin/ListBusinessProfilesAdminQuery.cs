using MediatR;

namespace TNK.UseCases.BusinessProfiles.ListAdmin;

/// <summary>
/// Represents a query to fetch a paginated list of Business Profiles for a SuperAdmin.
/// </summary>
public record ListBusinessProfilesAdminQuery : IRequest<Result<Common.Models.PagedResult<BusinessProfileDTO>>>
{
  public int? PageNumber { get; init; } = 1;
  public int PageSize { get; init; } = 10;
  public string? SearchTerm { get; init; }

  public ListBusinessProfilesAdminQuery(int? pageNumber = 1, int pageSize = 10, string? searchTerm = null)
  {
    PageNumber = pageNumber > 0 ? pageNumber : 1;
    PageSize = pageSize > 0 ? pageSize : 10;
    SearchTerm = searchTerm;
  }
}
