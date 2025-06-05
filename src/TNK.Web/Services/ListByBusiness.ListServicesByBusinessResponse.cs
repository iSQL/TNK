using TNK.UseCases.Services; // For ServiceDTO

namespace TNK.Web.Services;

public class ListServicesByBusinessResponse
{
  public List<ServiceDTO> Services { get; set; } = [];
  // Add pagination info if used:
  // public int PageNumber { get; set; }
  // public int PageSize { get; set; }
  // public int TotalRecords { get; set; }
  // public int TotalPages { get; set; }
}
