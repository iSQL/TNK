using System.Collections.Generic;

namespace TNK.UseCases.Common.Models;

public class PagedResult<T>
{
  public List<T> Items { get; }
  public int? PageNumber { get; }
  public int PageSize { get; }
  public int? TotalPages { get; }
  public int? TotalCount { get; }

  public bool HasPreviousPage => PageNumber > 1;
  public bool HasNextPage => PageNumber < TotalPages;

  public PagedResult(List<T> items, int count, int? pageNumber, int pageSize)
  {
    Items = items;
    TotalCount = count;
    PageSize = pageSize;
    PageNumber = pageNumber;
    TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);
  }
}
