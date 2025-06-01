// File: TNK.UseCases/Services/Specifications/ServicesByBusinessSpec.cs
using Ardalis.Specification;
using TNK.Core.ServiceManagementAggregate.Entities;

namespace TNK.UseCases.Services.Specifications;

public class ServicesByBusinessSpec : Specification<Service>
{
  public ServicesByBusinessSpec(int businessProfileId)
  {
    Query.Where(service => service.BusinessProfileId == businessProfileId)
         .OrderBy(service => service.Name); // Default ordering, can be adjusted or made dynamic
  }

  // You can add constructors for pagination if needed:
  // public ServicesByBusinessSpec(int businessProfileId, int skip, int take)
  // {
  //     Query.Where(service => service.BusinessProfileId == businessProfileId)
  //          .OrderBy(service => service.Name)
  //          .Skip(skip)
  //          .Take(take);
  // }
}
