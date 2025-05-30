using Ardalis.Specification;

namespace TNK.UseCases.BusinessProfiles.ListAdmin;

public class BusinessProfilesAdminSpec : Specification<Core.BusinessAggregate.BusinessProfile>
{
  public BusinessProfilesAdminSpec(int pageNumber, int pageSize, string? searchTerm)
  {
    
    int validPageNumber = pageNumber > 0 ? pageNumber : 1;
    int validPageSize = pageSize > 0 ? pageSize : 10; 

    Query.Skip((validPageNumber - 1) * validPageSize)
         .Take(validPageSize);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var lowerSearchTerm = searchTerm.Trim().ToLower();
      Query.Where(bp =>
          (bp.Name != null && bp.Name.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Address != null && bp.Address.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Description != null && bp.Description.ToLower().Contains(lowerSearchTerm)) ||
          (bp.VendorId != null && bp.VendorId.ToLower().Contains(lowerSearchTerm)) 
      );
    }

    Query.OrderBy(bp => bp.Name)
         .ThenBy(bp => bp.Id);
  }
}

// Separate specification for counting without pagination, only filtering.
public class BusinessProfilesAdminFilterSpec : Specification<Core.BusinessAggregate.BusinessProfile>
{
  public BusinessProfilesAdminFilterSpec(string? searchTerm)
  {
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var lowerSearchTerm = searchTerm.Trim().ToLower();
      Query.Where(bp =>
          (bp.Name != null && bp.Name.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Address != null && bp.Address.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Description != null && bp.Description.ToLower().Contains(lowerSearchTerm)) ||
          (bp.VendorId != null && bp.VendorId.ToLower().Contains(lowerSearchTerm))
      );
    }
  }
}
