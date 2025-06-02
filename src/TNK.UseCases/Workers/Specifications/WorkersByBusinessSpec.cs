using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities; // For Worker entity

namespace TNK.UseCases.Workers.Specifications;

public class WorkersByBusinessSpec : Specification<Worker>
{
  public WorkersByBusinessSpec(int businessProfileId)
  {
    Query.Where(worker => worker.BusinessProfileId == businessProfileId)
         .OrderBy(worker => worker.LastName) // Default order by last name
         .ThenBy(worker => worker.FirstName); // Then by first name
  }

  // Optional constructor for pagination (if you implement it)
  // public WorkersByBusinessSpec(int businessProfileId, int skip, int take)
  // {
  //     Query.Where(worker => worker.BusinessProfileId == businessProfileId)
  //          .OrderBy(worker => worker.LastName)
  //          .ThenBy(worker => worker.FirstName)
  //          .Skip(skip)
  //          .Take(take);
  // }
}
