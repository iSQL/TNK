namespace TNK.Web.Services;

public class ListServicesByBusinessRequest
{
  public const string Route = "/Businesses/{BusinessProfileId}/Services";
  public static string BuildRoute(int businessProfileId) => Route.Replace("{BusinessProfileId}", businessProfileId.ToString());

  public int BusinessProfileId { get; set; }
  // Add pagination parameters if needed:
  // public int? PageNumber { get; set; }
  // public int? PageSize { get; set; }
}
