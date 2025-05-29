// src/TNK.UseCases/BusinessProfiles/ListAdmin/BusinessProfilesAdminSpec.cs
using Ardalis.Specification;
using TNK.Core.BusinessAggregate; // For BusinessProfile entity

namespace TNK.UseCases.BusinessProfiles.ListAdmin;

public class BusinessProfilesAdminSpec : Specification<Core.BusinessAggregate.BusinessProfile>
{
  public BusinessProfilesAdminSpec(int pageNumber, int pageSize, string? searchTerm)
  {
    // Apply pagination
    // Ensure pageNumber is at least 1
    int validPageNumber = pageNumber > 0 ? pageNumber : 1;
    int validPageSize = pageSize > 0 ? pageSize : 10; // Default page size if invalid

    Query.Skip((validPageNumber - 1) * validPageSize)
         .Take(validPageSize);

    // Apply search filter if a search term is provided
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      var lowerSearchTerm = searchTerm.Trim().ToLower();
      Query.Where(bp =>
          (bp.Name != null && bp.Name.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Address != null && bp.Address.ToLower().Contains(lowerSearchTerm)) ||
          (bp.Description != null && bp.Description.ToLower().Contains(lowerSearchTerm)) ||
          (bp.VendorId != null && bp.VendorId.ToLower().Contains(lowerSearchTerm)) // Assuming VendorId is a string
      );
    }

    // Default ordering (optional, but good for consistent pagination)
    Query.OrderBy(bp => bp.Name)
         .ThenBy(bp => bp.Id); // Secondary sort for consistent ordering
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
